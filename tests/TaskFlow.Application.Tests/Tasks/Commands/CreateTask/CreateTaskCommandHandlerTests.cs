using NSubstitute;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Commands.CreateTask;
using TaskFlow.Domain.Entities;
using DomainTaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Application.Tests.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly CreateTaskCommandHandler _handler;

    public CreateTaskCommandHandlerTests()
    {
        _handler = new CreateTaskCommandHandler(_taskRepository, _currentUserContext);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTaskCommandHandler_WithValidInput_ReturnsTaskDto()
    {
        var ownerId = Guid.CreateVersion7();
        _currentUserContext.OwnerId.Returns(ownerId);
        var command = new CreateTaskCommand("Buy milk", "2% milk", DateTime.UtcNow.AddDays(1));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Buy milk", result.Title);
        Assert.Equal("2% milk", result.Description);
        Assert.Equal(DomainTaskStatus.Pending.ToString(), result.Status);
        Assert.Equal(ownerId, result.OwnerId);
        await _taskRepository.Received(1).AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTaskCommandHandler_AssignsOwnerIdFromCurrentUserContext()
    {
        var ownerId = Guid.CreateVersion7();
        _currentUserContext.OwnerId.Returns(ownerId);
        var command = new CreateTaskCommand("Valid title", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ownerId, result.OwnerId);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTaskCommandHandler_IgnoresClientSuppliedValues_UsesServerGenerated()
    {
        var ownerId = Guid.CreateVersion7();
        _currentUserContext.OwnerId.Returns(ownerId);
        var command = new CreateTaskCommand("Valid title", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(ownerId, result.OwnerId);
        Assert.Equal(DomainTaskStatus.Pending.ToString(), result.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTaskCommandHandler_SetsCreatedAtEqualToUpdatedAtOnCreation()
    {
        _currentUserContext.OwnerId.Returns(Guid.CreateVersion7());
        var command = new CreateTaskCommand("Valid title", null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(result.CreatedAt, result.UpdatedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTaskCommandHandler_NormalizesEmptyDescriptionToNull()
    {
        _currentUserContext.OwnerId.Returns(Guid.CreateVersion7());
        var command = new CreateTaskCommand("Valid title", string.Empty, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Null(result.Description);
    }
}
