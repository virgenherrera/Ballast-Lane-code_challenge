using NSubstitute;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Commands.DeleteTask;

namespace TaskFlow.Application.Tests.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly DeleteTaskCommandHandler _handler;

    public DeleteTaskCommandHandlerTests()
    {
        _handler = new DeleteTaskCommandHandler(_taskRepository, _currentUserContext);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteTaskCommandHandler_TaskExistsAndOwned_CallsDeleteAsyncOnce()
    {
        var ownerId = Guid.CreateVersion7();
        var taskId = Guid.CreateVersion7();
        _currentUserContext.OwnerId.Returns(ownerId);
        _taskRepository.DeleteAsync(taskId, ownerId, Arg.Any<CancellationToken>()).Returns(true);

        var command = new DeleteTaskCommand(taskId);

        await _handler.Handle(command, CancellationToken.None);

        await _taskRepository.Received(1).DeleteAsync(taskId, ownerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteTaskCommandHandler_RepositoryReturnsFalse_ThrowsTaskNotFoundException()
    {
        var ownerId = Guid.CreateVersion7();
        var taskId = Guid.CreateVersion7();
        _currentUserContext.OwnerId.Returns(ownerId);
        _taskRepository.DeleteAsync(taskId, ownerId, Arg.Any<CancellationToken>()).Returns(false);

        var command = new DeleteTaskCommand(taskId);

        await Assert.ThrowsAsync<TaskNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
