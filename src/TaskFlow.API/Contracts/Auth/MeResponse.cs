namespace TaskFlow.API.Contracts.Auth;

/// <summary>
/// Outbound representation of the authenticated caller's own profile,
/// returned by GET /api/auth/me. Same field set as <see cref="RegisterResponse"/>
/// — deliberately excludes PasswordHash and any raw password.
/// </summary>
public sealed record MeResponse(Guid Id, string Email, string Name, DateTime CreatedAt);
