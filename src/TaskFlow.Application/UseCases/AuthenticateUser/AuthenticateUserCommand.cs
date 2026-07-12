namespace TaskFlow.Application.UseCases.AuthenticateUser;

public sealed record AuthenticateUserCommand(string Email, string Password);
