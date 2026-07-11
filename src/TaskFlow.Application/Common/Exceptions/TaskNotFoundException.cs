namespace TaskFlow.Application.Common.Exceptions;

public sealed class TaskNotFoundException : Exception
{
    public TaskNotFoundException(Guid taskId)
        : base($"Task '{taskId}' was not found.")
    { }
}
