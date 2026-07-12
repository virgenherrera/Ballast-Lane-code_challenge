namespace TaskFlow.Infrastructure.Security;

public sealed class BcryptOptions
{
    public const string SectionName = "Bcrypt";

    /// <summary>
    /// BCrypt work factor (log2 rounds). 12 in production (Decision #3),
    /// 4 in tests for speed. Bound from configuration, defaults to 12
    /// when unset so production never silently runs at test strength.
    /// </summary>
    public int WorkFactor { get; set; } = 12;
}
