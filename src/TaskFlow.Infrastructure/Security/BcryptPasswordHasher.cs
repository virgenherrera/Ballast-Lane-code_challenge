using Microsoft.Extensions.Options;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Infrastructure.Security;

/// <summary>
/// BCrypt-backed <see cref="IPasswordHasher"/> adapter. Work factor is configurable
/// via <see cref="BcryptOptions"/> (Decision #3: 12 in production, 4 in tests).
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private readonly int _workFactor;

    public BcryptPasswordHasher(IOptions<BcryptOptions> options)
    {
        _workFactor = options.Value.WorkFactor;
    }

    public PasswordHash Hash(string plainTextPassword)
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: _workFactor);
        return PasswordHash.Create(hashed);
    }

    public bool Verify(string plainTextPassword, PasswordHash passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash.Value);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Malformed/foreign hash format — treat as a non-match rather than
            // propagating, since PasswordHash.Create already guarantees the hash
            // this hasher wrote is well-formed. Any other exception type propagates.
            return false;
        }
    }
}
