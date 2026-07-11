namespace TaskFlow.Application.Tasks.Queries.GetTaskById;

public sealed record GetTaskByIdQuery(Guid TaskId, Guid OwnerId);
