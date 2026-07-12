namespace TaskFlow.Domain.Interfaces;

using TaskFlow.Domain.Entities;
using TaskFlow.Domain.ValueObjects;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken);
}
