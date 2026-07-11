using NSubstitute;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Commands.UpdateTask;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tests.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly UpdateTaskCommandHandler _handler;

    public UpdateTaskCommandHandlerTests()
    {
        _handler = new UpdateTaskCommandHandler(_taskRepository, _currentUserContext);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTaskCommandHandler_ForTaskOwnedByAnotherUser_ThrowsNotFound()
    {
        var ownerId = Guid.CreateVersion7();
        var otherOwnerId = Guid.CreateVersion7();
        _currentUserContext.OwnerId.Returns(ownerId);

        var existingTask = TaskItem.Create("Test", null, null, otherOwnerId);
        _taskRepository.GetByIdAsync(existingTask.Id, Arg.Any<CancellationToken>()).Returns(existingTask);

        var command = new UpdateTaskCommand(existingTask.Id, "New title", null, null, null);

        await Assert.ThrowsAsync<TaskNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        await _taskRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTaskCommandHandler_WithPartialFields_OnlyUpdatesSpecifiedFieldsPreservingRest()
    {
        var ownerId = Guid.CreateVersion7();
        _currentUserContext.OwnerId.Returns(ownerId);

        var existingTask = TaskItem.Create("Original title", "Original description", null, ownerId);
        _taskRepository.GetByIdAsync(existingTask.Id, Arg.Any<CancellationToken>()).Returns(existingTask);

        var command = new UpdateTaskCommand(existingTask.Id, "Updated title", null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Updated title", result.Title);
        Assert.Equal("Original description", result.Description);
        await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTaskCommandHandler_WithPastDueDate_SucceedsWithoutValidationError()
    {
        var ownerId = Guid.CreateVersion7();
        _currentUserContext.OwnerId.Returns(ownerId);

        var existingTask = TaskItem.Create("Test", null, null, ownerId);
        _taskRepository.GetByIdAsync(existingTask.Id, Arg.Any<CancellationToken>()).Returns(existingTask);

        var pastDate = DateTime.UtcNow.AddDays(-5);
        var command = new UpdateTaskCommand(existingTask.Id, null, null, null, pastDate);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(pastDate, result.DueDate);
        await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
