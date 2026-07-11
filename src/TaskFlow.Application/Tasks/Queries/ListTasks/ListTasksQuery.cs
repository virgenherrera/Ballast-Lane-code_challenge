namespace TaskFlow.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Query for the paginated, optionally status-filtered task list
/// (US-005, US-009). <see cref="Status"/> and pagination fields are the raw,
/// still-unvalidated wire values — validation happens via
/// <see cref="ListTasksQueryValidator"/> before <see cref="ListTasksQueryHandler"/>
/// runs.
/// </summary>
public sealed record ListTasksQuery(
    Guid OwnerId, string? Status, int? Page, int? PerPage);
