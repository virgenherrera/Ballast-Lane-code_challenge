namespace TaskFlow.Domain.Exceptions;

public sealed class InvalidTaskTitleException : DomainException
{
    public InvalidTaskTitleException(string message) : base(message)
    {
    }
}
