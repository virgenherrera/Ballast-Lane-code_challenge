using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken ct);

    // TEMPORARY: returns all tasks with no filtering/sorting/pagination.
    // Superseded by US-005 (list/filter/paginate tasks).
    Task<List<TaskItem>> GetAllAsync(CancellationToken ct);
}
