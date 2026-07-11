namespace TaskFlow.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Slim list-row projection (4 fields) — deliberately narrower than
/// <see cref="Dtos.TaskDto"/>, which is reserved for the detail endpoint
/// (US-005 AC-005.4).
/// </summary>
public sealed record TaskListItemDto(
    Guid Id, string Title, string Status, DateTime? DueDate);
