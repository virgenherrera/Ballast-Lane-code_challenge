using NSubstitute;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Queries.GetTaskById;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tests.Tasks.Queries.GetTaskById;

public class GetTaskByIdQueryHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly GetTaskByIdQueryHandler _handler;

    public GetTaskByIdQueryHandlerTests()
    {
        _handler = new GetTaskByIdQueryHandler(_taskRepository);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_ForOwnedTask_ReturnsFullTaskDto()
    {
        var ownerId = Guid.CreateVersion7();
        var task = TaskItem.Create("Buy milk", "2%", null, ownerId);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var query = new GetTaskByIdQuery(task.Id, ownerId);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(task.Id, result.Id);
        Assert.Equal("Buy milk", result.Title);
        Assert.Equal("2%", result.Description);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(ownerId, result.OwnerId);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_ForNonExistentTask_ThrowsTaskNotFoundException()
    {
        var ownerId = Guid.CreateVersion7();
        var taskId = Guid.CreateVersion7();
        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        var query = new GetTaskByIdQuery(taskId, ownerId);

        await Assert.ThrowsAsync<TaskNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_ForTaskOwnedByAnotherUser_ThrowsTaskNotFoundException()
    {
        var ownerId = Guid.CreateVersion7();
        var otherOwnerId = Guid.CreateVersion7();
        var task = TaskItem.Create("Someone else's task", null, null, otherOwnerId);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var query = new GetTaskByIdQuery(task.Id, ownerId);

        await Assert.ThrowsAsync<TaskNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_ForTaskWithInProgressStatus_MapsToDisplayStringWithSpace()
    {
        var ownerId = Guid.CreateVersion7();
        var task = TaskItem.Create("Task", null, null, ownerId);
        task.ChangeStatus(TaskFlow.Domain.Enums.TaskStatus.InProgress);
        _taskRepository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var query = new GetTaskByIdQuery(task.Id, ownerId);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal("In Progress", result.Status);
    }
}
