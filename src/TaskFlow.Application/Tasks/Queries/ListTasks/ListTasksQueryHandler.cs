using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Mapping;
using TaskFlow.Application.Common.Pagination;

namespace TaskFlow.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Handles <see cref="ListTasksQuery"/>: resolves defaults, converts the wire
/// status string to the domain enum (BEFORE it reaches the repository's
/// <c>IQueryable</c> — EF Core cannot translate <c>TaskStatusMapper.ParseOrNull</c>
/// itself), delegates filtering/paging to <see cref="ITaskRepository.ListAsync"/>,
/// and builds prev/next links via <see cref="PagingLinkBuilder"/>.
/// </summary>
public sealed class ListTasksQueryHandler
{
    private readonly ITaskRepository _taskRepository;

    public ListTasksQueryHandler(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<ListTasksResult> Handle(ListTasksQuery query, CancellationToken ct)
    {
        var page = query.Page ?? PaginationDefaults.DefaultPage;
        var perPage = query.PerPage ?? PaginationDefaults.DefaultPerPage;
        var status = TaskStatusMapper.ParseOrNull(query.Status);

        var (items, total) = await _taskRepository.ListAsync(
            query.OwnerId, status, page, perPage, ct);

        var dtoItems = items
            .Select(task => new TaskListItemDto(
                task.Id,
                task.Title,
                TaskStatusMapper.ToDisplayString(task.Status),
                task.DueDate))
            .ToList();

        var (prev, next) = PagingLinkBuilder.Build(page, perPage, total, query.Status);

        var paging = new PagingInfo(page, perPage, total, prev, next);

        return new ListTasksResult(dtoItems, paging);
    }
}
