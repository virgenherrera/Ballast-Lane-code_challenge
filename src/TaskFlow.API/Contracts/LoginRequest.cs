namespace TaskFlow.API.Contracts;

/// <summary>
/// Inbound payload for POST /api/auth/login.
/// </summary>
public sealed record LoginRequest(string Email, string Password);
