using NSubstitute;
using TaskFlow.Application.UseCases.AuthenticateUser;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Application.Tests.UseCases.AuthenticateUser;

public class AuthenticateUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AuthenticateUserHandler _handler;

    public AuthenticateUserHandlerTests()
    {
        _handler = new AuthenticateUserHandler(_userRepository, _passwordHasher, _tokenService);
    }

    private static User CreateUser(string email = "user@example.com", string name = "Jane Doe")
    {
        return User.Create(
            Email.Create(email),
            name,
            PasswordHash.Create("$2a$12$abcdefghijklmnopqrstuuABCDEFGHIJKLMNOPQRSTUV1234567890"));
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_ValidCredentials_ReturnsTokenAndUserSummary()
    {
        var user = CreateUser();
        var command = new AuthenticateUserCommand(user.Email.Value, "correct-password");

        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);
        _tokenService.GenerateToken(user).Returns("signed-jwt-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("signed-jwt-token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(900, result.ExpiresIn);
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal(user.Email.Value, result.User.Email);
        Assert.Equal(user.Name, result.User.Name);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_ValidCredentials_CallsVerifyWithCorrectArgs()
    {
        var user = CreateUser();
        var command = new AuthenticateUserCommand(user.Email.Value, "correct-password");

        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);
        _tokenService.GenerateToken(user).Returns("signed-jwt-token");

        await _handler.Handle(command, CancellationToken.None);

        _passwordHasher.Received(1).Verify(command.Password, user.PasswordHash);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_UserNotFound_ThrowsInvalidCredentialsException()
    {
        var command = new AuthenticateUserCommand("missing@example.com", "any-password");

        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<PasswordHash>()).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None));

        _tokenService.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var user = CreateUser();
        var command = new AuthenticateUserCommand(user.Email.Value, "wrong-password");

        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None));

        _tokenService.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_UserNotFoundVsWrongPassword_SameExceptionMessage()
    {
        var user = CreateUser();

        _userRepository.GetByEmailAsync(Arg.Is<Email>(e => e.Value == "missing@example.com"), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository.GetByEmailAsync(Arg.Is<Email>(e => e.Value == user.Email.Value), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<PasswordHash>()).Returns(false);

        var notFoundCommand = new AuthenticateUserCommand("missing@example.com", "any-password");
        var wrongPasswordCommand = new AuthenticateUserCommand(user.Email.Value, "wrong-password");

        var notFoundException = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(notFoundCommand, CancellationToken.None));
        var wrongPasswordException = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(wrongPasswordCommand, CancellationToken.None));

        Assert.Equal(notFoundException.Message, wrongPasswordException.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_UserNotFound_StillCallsPasswordHasherVerify()
    {
        var command = new AuthenticateUserCommand("missing@example.com", "any-password");

        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<PasswordHash>()).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(command, CancellationToken.None));

        _passwordHasher.Received(1).Verify(Arg.Any<string>(), Arg.Any<PasswordHash>());
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_FailurePaths_NeverCallTokenService()
    {
        var user = CreateUser();

        _userRepository.GetByEmailAsync(Arg.Is<Email>(e => e.Value == "missing@example.com"), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository.GetByEmailAsync(Arg.Is<Email>(e => e.Value == user.Email.Value), Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<PasswordHash>()).Returns(false);

        var notFoundCommand = new AuthenticateUserCommand("missing@example.com", "any-password");
        var wrongPasswordCommand = new AuthenticateUserCommand(user.Email.Value, "wrong-password");

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(notFoundCommand, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _handler.Handle(wrongPasswordCommand, CancellationToken.None));

        _tokenService.DidNotReceive().GenerateToken(Arg.Any<User>());
    }
}
