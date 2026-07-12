namespace TaskFlow.API.Contracts;

/// <summary>
/// Outbound representation of a newly registered user. Deliberately excludes
/// PasswordHash and any raw password — never expose credentials in a response.
/// </summary>
public sealed record RegisterResponse(Guid Id, string Email, string Name, DateTime CreatedAt);
