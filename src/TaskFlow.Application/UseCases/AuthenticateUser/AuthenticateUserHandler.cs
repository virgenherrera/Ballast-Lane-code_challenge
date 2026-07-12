using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Application.UseCases.AuthenticateUser;

public sealed class AuthenticateUserHandler
{
    // Named constant, single source of truth — do not hardcode 900 elsewhere.
    private const int ExpiresInSeconds = 900;

    // A syntactically-valid but never-matched hash, used ONLY to keep the Verify() call
    // shape identical when the user does not exist. This value never corresponds to any
    // real user's stored hash.
    private static readonly PasswordHash DummyHash = PasswordHash.Create(
        "$2a$12$CwTycUXWue0Thq9StjUM0uJ8Nlq/HJ/PXtL5DsAmxOM.MRp7z3Y0i");

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthenticateUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthenticateUserResult> Handle(
        AuthenticateUserCommand command,
        CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // CRITICAL (AC-002.4 / Decision #9): Verify MUST execute on EVERY path, including
        // when user is null, so both failure paths take the same time. Do NOT short-circuit
        // before this line.
        var hashToVerifyAgainst = user?.PasswordHash ?? DummyHash;
        var isValid = _passwordHasher.Verify(command.Password, hashToVerifyAgainst);

        if (user is null || !isValid)
        {
            // Both failure branches throw the SAME exception type/message — do not
            // differentiate wording between "user not found" and "wrong password".
            throw new InvalidCredentialsException();
        }

        var token = _tokenService.GenerateToken(user);

        return new AuthenticateUserResult(
            token,
            "Bearer",
            ExpiresInSeconds,
            new AuthenticatedUserSummary(user.Id, user.Email.Value, user.Name));
    }
}
