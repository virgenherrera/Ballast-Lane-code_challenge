namespace TaskFlow.Domain.Interfaces;

using TaskFlow.Domain.ValueObjects;

public interface IPasswordHasher
{
    PasswordHash Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, PasswordHash passwordHash);
}
