using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _dbContext;

    public TaskRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct)
    {
        await _dbContext.Tasks.AddAsync(task, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<TaskItem> Items, int Total)> ListAsync(
        Guid ownerId, Domain.Enums.TaskStatus? status,
        int page, int perPage, CancellationToken ct)
    {
        // Conditional LINQ composition (C# `if` branching), NOT an `OR`
        // pattern inside a single `.Where()` — the OR form can inhibit
        // PostgreSQL index usage on (owner_id, created_at, id).
        var query = _dbContext.Tasks.AsNoTracking().Where(t => t.OwnerId == ownerId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct)
    {
        var rowsAffected = await _dbContext.Tasks
            .Where(t => t.Id == id && t.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct);

        return rowsAffected > 0;
    }
}
