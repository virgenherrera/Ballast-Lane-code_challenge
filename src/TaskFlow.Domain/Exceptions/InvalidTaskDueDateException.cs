namespace TaskFlow.Domain.Exceptions;

public sealed class InvalidTaskDueDateException : DomainException
{
    public InvalidTaskDueDateException(string message) : base(message)
    {
    }
}
