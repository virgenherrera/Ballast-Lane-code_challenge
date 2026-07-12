using System.Text.RegularExpressions;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.ValueObjects;

public sealed partial class Email : IEquatable<Email>
{
    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidEmailException("Email is required.");
        }

        if (value.Length > MaxLength)
        {
            throw new InvalidEmailException("Email exceeds maximum length of 254 characters.");
        }

        // Check casing BEFORE format, so an uppercase invalid-format email still reports
        // the casing violation (defense-in-depth per Decision #4 — reject, do not normalize).
        if (value != value.ToLowerInvariant())
        {
            throw new InvalidEmailException("Email must not contain uppercase characters.");
        }

        if (!EmailFormatRegex().IsMatch(value))
        {
            throw new InvalidEmailException("Email format is invalid.");
        }

        return new Email(value);
    }

    public override bool Equals(object? obj) => Equals(obj as Email);

    public bool Equals(Email? other) => other is not null && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailFormatRegex();
}
