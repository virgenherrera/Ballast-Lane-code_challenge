using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Tests.Exceptions;

public class DuplicateEmailExceptionTests
{
    [Fact]
    public void DuplicateEmailException_InheritsDomainException()
    {
        var exception = new DuplicateEmailException();

        Assert.IsAssignableFrom<DomainException>(exception);
    }

    [Fact]
    public void DuplicateEmailException_DefaultMessage_IsSet()
    {
        var exception = new DuplicateEmailException();

        Assert.Equal("An account with this email already exists.", exception.Message);
    }
}
