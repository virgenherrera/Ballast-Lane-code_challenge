using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken ct);

    /// <summary>
    /// Returns a single owner-scoped, optionally status-filtered page of
    /// tasks ordered by <c>CreatedAt DESC, Id DESC</c> (US-005, US-009),
    /// alongside the total count of rows matching the filter (BEFORE paging
    /// is applied) so callers can build paging metadata.
    /// </summary>
    Task<(IReadOnlyList<TaskItem> Items, int Total)> ListAsync(
        Guid ownerId, Domain.Enums.TaskStatus? status,
        int page, int perPage, CancellationToken ct);

    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);

    Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct);
}
