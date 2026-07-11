namespace TaskFlow.API.Contracts;

/// <summary>
/// Outbound representation of a task. Status is serialized as a string
/// (e.g. "Pending"), never the underlying enum's numeric value.
/// </summary>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTime? DueDate,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
