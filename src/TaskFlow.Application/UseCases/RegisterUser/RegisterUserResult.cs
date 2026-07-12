namespace TaskFlow.Application.UseCases.RegisterUser;

public sealed record RegisterUserResult(Guid Id, string Email, string Name, DateTime CreatedAt);
