namespace TaskFlow.API.Configuration;

/// <summary>
/// Bound from the "Jwt" configuration section. Populated in
/// <c>Program.cs</c> from environment variables. Not consumed yet —
/// JWT authentication middleware is added in a later batch.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}
