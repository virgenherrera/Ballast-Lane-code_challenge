namespace TaskFlow.API.Contracts;

/// <summary>
/// Inbound payload for POST /api/auth/register. Deliberately has NO Id or
/// CreatedAt properties — those are server-assigned.
/// </summary>
public sealed record RegisterRequest(string Email, string Name, string Password);
