namespace TaskFlow.Application.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    DateTime? DueDate);
