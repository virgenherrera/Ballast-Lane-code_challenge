namespace TaskFlow.API.Contracts;

public sealed record TaskListResponse(
    IReadOnlyList<TaskListItemResponse> Items, PagingResponse Paging);
