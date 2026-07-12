using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Domain.Entities;

public sealed class User
{
    public Guid Id { get; }
    public Email Email { get; private set; }
    public string Name { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public DateTime CreatedAt { get; }

    private User(Guid id, Email email, string name, PasswordHash passwordHash, DateTime createdAt)
    {
        Id = id;
        Email = email;
        Name = name;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public static User Create(Email email, string name, PasswordHash passwordHash)
    {
        var trimmedName = name?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new InvalidUserNameException("Name is required.");
        }

        var id = Guid.CreateVersion7();
        var now = DateTime.UtcNow;

        return new User(id, email, trimmedName, passwordHash, now);
    }
}
