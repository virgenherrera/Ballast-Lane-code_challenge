using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Specifications;

public static class TaskOwnershipSpecification
{
    public static TaskItem EnsureOwnedBy(TaskItem? task, Guid ownerId, Guid requestedTaskId)
    {
        if (task is null || task.OwnerId != ownerId)
            throw new TaskNotFoundException(requestedTaskId);
        return task;
    }
}
