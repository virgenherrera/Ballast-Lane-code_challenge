namespace TaskFlow.API.Contracts;

/// <summary>
/// Inbound payload for PATCH /api/tasks/{id}. All fields are optional —
/// only fields present in the request are applied (partial update). This is
/// a dedicated DTO with no shared base with <see cref="CreateTaskRequest"/>.
/// </summary>
public sealed record UpdateTaskRequest(
    string? Title,
    string? Description,
    string? Status,
    DateTime? DueDate);
