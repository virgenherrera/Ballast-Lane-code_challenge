namespace TaskFlow.Application.Tasks.Queries.ListTasks;

public sealed record PagingInfo(
    int Page, int PerPage, int Total, string? Prev, string? Next);
