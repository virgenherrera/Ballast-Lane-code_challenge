using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Tests.Exceptions;

public class InvalidCredentialsExceptionTests
{
    [Fact]
    public void InvalidCredentialsException_InheritsDomainException()
    {
        var exception = new InvalidCredentialsException();

        Assert.IsAssignableFrom<DomainException>(exception);
    }

    [Fact]
    public void InvalidCredentialsException_Message_DoesNotContainFieldHints()
    {
        var exception = new InvalidCredentialsException();
        var message = exception.Message.ToLowerInvariant();

        Assert.DoesNotContain("email", message);
        Assert.DoesNotContain("password", message);
        Assert.DoesNotContain("user", message);
    }

    [Fact]
    public void InvalidCredentialsException_Message_IsGenericConstant()
    {
        var exception = new InvalidCredentialsException();

        Assert.Equal(InvalidCredentialsException.GenericMessage, exception.Message);
    }
}
