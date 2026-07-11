using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Specifications;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tests.Common.Specifications;

public class TaskOwnershipSpecificationTests
{
    [Fact]
    public void TaskOwnershipSpecification_EnsureOwnedBy_WithNullTask_ThrowsTaskNotFoundException()
    {
        var ownerId = Guid.CreateVersion7();
        var requestedTaskId = Guid.CreateVersion7();

        Assert.Throws<TaskNotFoundException>(
            () => TaskOwnershipSpecification.EnsureOwnedBy(null, ownerId, requestedTaskId));
    }

    [Fact]
    public void TaskOwnershipSpecification_EnsureOwnedBy_WithMismatchedOwnerId_ThrowsTaskNotFoundException()
    {
        var ownerId = Guid.CreateVersion7();
        var otherOwnerId = Guid.CreateVersion7();
        var task = TaskItem.Create("Buy milk", null, null, otherOwnerId);

        Assert.Throws<TaskNotFoundException>(
            () => TaskOwnershipSpecification.EnsureOwnedBy(task, ownerId, task.Id));
    }

    [Fact]
    public void TaskOwnershipSpecification_EnsureOwnedBy_WithMatchingOwnerId_ReturnsTask()
    {
        var ownerId = Guid.CreateVersion7();
        var task = TaskItem.Create("Buy milk", null, null, ownerId);

        var result = TaskOwnershipSpecification.EnsureOwnedBy(task, ownerId, task.Id);

        Assert.Same(task, result);
    }
}
