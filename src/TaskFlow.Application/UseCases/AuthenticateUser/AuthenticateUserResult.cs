namespace TaskFlow.Application.UseCases.AuthenticateUser;

public sealed record AuthenticateUserResult(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    AuthenticatedUserSummary User);

public sealed record AuthenticatedUserSummary(Guid Id, string Email, string Name);
