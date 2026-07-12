using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Email_Create_ValidLowercaseAddress_Succeeds()
    {
        var email = Email.Create("jane@example.com");

        Assert.Equal("jane@example.com", email.Value);
    }

    [Fact]
    public void Email_Create_UppercaseInput_ThrowsDomainException()
    {
        Assert.Throws<InvalidEmailException>(() => Email.Create("Jane@example.com"));
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("foo@")]
    [InlineData("@bar.com")]
    public void Email_Create_InvalidFormat_ThrowsDomainException(string value)
    {
        Assert.Throws<InvalidEmailException>(() => Email.Create(value));
    }

    [Fact]
    public void Email_Create_ExceedsMaxLength_ThrowsDomainException()
    {
        var localPart = new string('a', 255 - "@b.co".Length);
        var value = $"{localPart}@b.co";

        Assert.True(value.Length > Email.MaxLength);
        Assert.Throws<InvalidEmailException>(() => Email.Create(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Email_Create_NullOrWhitespace_ThrowsDomainException(string? value)
    {
        Assert.Throws<InvalidEmailException>(() => Email.Create(value!));
    }

    [Fact]
    public void Email_Equals_SameValue_ReturnsTrue()
    {
        var first = Email.Create("jane@example.com");
        var second = Email.Create("jane@example.com");

        Assert.True(first.Equals(second));
    }

    [Fact]
    public void Email_Equals_DifferentValue_ReturnsFalse()
    {
        var first = Email.Create("jane@example.com");
        var second = Email.Create("john@example.com");

        Assert.False(first.Equals(second));
    }
}
