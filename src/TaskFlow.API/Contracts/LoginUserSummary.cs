namespace TaskFlow.API.Contracts;

/// <summary>
/// Minimal user summary embedded in <see cref="LoginResponse"/>. Deliberately
/// excludes PasswordHash and any raw password — never expose credentials in a
/// response.
/// </summary>
public sealed record LoginUserSummary(Guid Id, string Email, string Name);
