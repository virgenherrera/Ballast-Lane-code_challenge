namespace TaskFlow.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    Guid OwnerId { get; }
}
