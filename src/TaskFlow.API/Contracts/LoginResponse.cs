namespace TaskFlow.API.Contracts;

/// <summary>
/// Outbound representation of a successful login. <see cref="ExpiresIn"/> is
/// sourced from <c>AuthenticateUserResult.ExpiresIn</c> (named constant in
/// the handler) — never hardcode this value separately here.
/// </summary>
public sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresIn, LoginUserSummary User);
