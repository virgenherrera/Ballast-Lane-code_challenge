using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.ValueObjects;

public sealed class PasswordHash
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static PasswordHash Create(string hashedValue)
    {
        if (string.IsNullOrWhiteSpace(hashedValue))
        {
            throw new InvalidPasswordHashException("Password hash is required.");
        }

        return new PasswordHash(hashedValue);
    }

    // Deliberately DOES NOT override ToString() to expose Value — inherits Object.ToString()
    // so accidental interpolation/logging never leaks the hash. Do NOT add a ToString() override.
}
