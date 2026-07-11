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

    // TEMPORARY: no filtering/sorting/pagination. Superseded by US-005.
    public async Task<List<TaskItem>> GetAllAsync(CancellationToken ct)
    {
        return await _dbContext.Tasks.AsNoTracking().ToListAsync(ct);
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
