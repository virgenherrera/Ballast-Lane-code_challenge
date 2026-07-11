namespace TaskFlow.API.Contracts;

/// <summary>
/// Slim list-row representation (4 fields) returned by <c>GET /api/tasks</c>
/// (US-005 AC-005.4). Status is serialized as a string (e.g. "In Progress"),
/// never the underlying enum's numeric value.
/// </summary>
public sealed record TaskListItemResponse(
    Guid Id, string Title, string Status, DateTime? DueDate);
