namespace TaskFlow.Application.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand(
    Guid Id,
    string? Title,
    string? Description,
    string? Status,
    DateTime? DueDate);
