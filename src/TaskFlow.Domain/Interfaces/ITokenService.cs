namespace TaskFlow.Domain.Interfaces;

using TaskFlow.Domain.Entities;

public interface ITokenService
{
    string GenerateToken(User user);
}
