namespace TaskFlow.Application.Tasks.Queries.ListTasks;

public sealed record ListTasksResult(
    IReadOnlyList<TaskListItemDto> Items, PagingInfo Paging);
