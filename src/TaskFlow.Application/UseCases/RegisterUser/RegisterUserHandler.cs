using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Application.UseCases.RegisterUser;

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        // Defense-in-depth: RegisterUserValidator (FluentValidation) runs before this
        // handler in the real pipeline (Batch 3 wires it), but the handler still
        // constructs the Email VO directly here.
        var email = Email.Create(command.Email);

        // MUST throw before hashing or persisting (AC-001.2) — do not hash on a
        // duplicate email, both for correctness and to avoid wasted BCrypt work.
        if (await _userRepository.ExistsAsync(email, cancellationToken))
        {
            throw new DuplicateEmailException();
        }

        var passwordHash = _passwordHasher.Hash(command.Password);
        var user = User.Create(email, command.Name, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

        return new RegisterUserResult(user.Id, user.Email.Value, user.Name, user.CreatedAt);
    }
}
