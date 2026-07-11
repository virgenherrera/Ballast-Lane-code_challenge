namespace TaskFlow.Application.Tasks.Dtos;

public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTime? DueDate,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
