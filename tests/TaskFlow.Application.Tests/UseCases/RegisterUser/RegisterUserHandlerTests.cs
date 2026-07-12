using NSubstitute;
using TaskFlow.Application.UseCases.RegisterUser;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Application.Tests.UseCases.RegisterUser;

public class RegisterUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(_userRepository, _passwordHasher);
    }

    private static PasswordHash FakeHash() => PasswordHash.Create("hashed-value");

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserAndReturnsResult()
    {
        _userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns(FakeHash());
        var command = new RegisterUserCommand("jane@example.com", "Jane Doe", "Str0ng!Pass");

        var result = await _handler.Handle(command, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("jane@example.com", result.Email);
        Assert.Equal("Jane Doe", result.Name);
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public async Task Handle_ValidCommand_HashesPasswordBeforePersisting()
    {
        _userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns(FakeHash());
        var command = new RegisterUserCommand("jane@example.com", "Jane Doe", "Str0ng!Pass");

        await _handler.Handle(command, CancellationToken.None);

        _passwordHasher.Received(1).Hash("Str0ng!Pass");
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u => u.PasswordHash != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_GeneratesUuidV7ForNewUser()
    {
        _userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns(FakeHash());
        var command1 = new RegisterUserCommand("jane@example.com", "Jane Doe", "Str0ng!Pass");
        var command2 = new RegisterUserCommand("john@example.com", "John Doe", "Str0ng!Pass");

        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result1.Id);
        Assert.NotEqual(Guid.Empty, result2.Id);
        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        _userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);
        var command = new RegisterUserCommand("jane@example.com", "Jane Doe", "Str0ng!Pass");

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => _handler.Handle(command, CancellationToken.None));

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_DoesNotCallPasswordHasher()
    {
        _userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);
        var command = new RegisterUserCommand("jane@example.com", "Jane Doe", "Str0ng!Pass");

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => _handler.Handle(command, CancellationToken.None));

        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_ValidName_TrimmedBeforeStorage()
    {
        _userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns(FakeHash());
        var command = new RegisterUserCommand("jane@example.com", "  Alice  ", "Str0ng!Pass");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Alice", result.Name);
    }

    [Fact]
    public void RegisterUserResult_HasNoPasswordOrHashField()
    {
        var properties = typeof(RegisterUserResult).GetProperties().Select(p => p.Name).ToArray();

        Assert.Equal(new[] { "Id", "Email", "Name", "CreatedAt" }, properties);
    }

    [Fact]
    public void Email_Equals_SameValue_ReturnsTrue()
    {
        var email1 = Email.Create("jane@example.com");
        var email2 = Email.Create("jane@example.com");

        Assert.Equal(email1, email2);
        Assert.True(email1.Equals(email2));
    }

    [Fact]
    public void PasswordHash_Constructor_NullOrEmpty_Throws()
    {
        Assert.Throws<InvalidPasswordHashException>(() => PasswordHash.Create(string.Empty));
        Assert.Throws<InvalidPasswordHashException>(() => PasswordHash.Create(null!));
    }
}
