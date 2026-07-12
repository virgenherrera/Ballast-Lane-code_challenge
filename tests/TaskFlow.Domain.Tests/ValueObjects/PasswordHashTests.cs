using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Domain.Tests.ValueObjects;

public class PasswordHashTests
{
    [Fact]
    public void PasswordHash_Create_ValidValue_Succeeds()
    {
        var passwordHash = PasswordHash.Create("hashed-value");

        Assert.Equal("hashed-value", passwordHash.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PasswordHash_Create_NullOrEmpty_ThrowsDomainException(string? value)
    {
        Assert.Throws<InvalidPasswordHashException>(() => PasswordHash.Create(value!));
    }

    [Fact]
    public void PasswordHash_ToString_DoesNotExposeRawValue()
    {
        var passwordHash = PasswordHash.Create("super-secret-hash");

        var result = passwordHash.ToString();

        Assert.DoesNotContain("super-secret-hash", result);
    }
}
