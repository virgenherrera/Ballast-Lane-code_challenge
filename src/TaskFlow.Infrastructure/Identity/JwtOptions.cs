namespace TaskFlow.Infrastructure.Identity;

/// <summary>
/// Bound from the "Jwt" configuration section. Populated in
/// <c>Program.cs</c> from environment variables and consumed by
/// <see cref="JwtTokenService"/> to issue HS256-signed access tokens.
/// Lives in Infrastructure (not API.Configuration) so the concrete
/// <see cref="JwtTokenService"/> adapter can depend on it directly
/// without introducing a circular Infrastructure -&gt; API project
/// reference (API already references Infrastructure).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    // Single source of truth for the access-token lifetime (15 minutes).
    // Do NOT hardcode 900 anywhere else — read this property instead.
    public int ExpirySeconds { get; set; } = 900;
}
