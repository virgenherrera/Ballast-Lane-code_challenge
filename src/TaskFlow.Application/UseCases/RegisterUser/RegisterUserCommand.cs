namespace TaskFlow.Application.UseCases.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Name, string Password);
