namespace TaskFlow.Domain.Exceptions;

public sealed class InvalidUserNameException : DomainException
{
    public InvalidUserNameException(string message) : base(message)
    {
    }
}
