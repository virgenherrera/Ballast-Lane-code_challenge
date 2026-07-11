using NSubstitute;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Queries.ListTasks;
using TaskFlow.Domain.Entities;
using DomainTaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Application.Tests.Tasks.Queries.ListTasks;

public class ListTasksQueryHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly ListTasksQueryHandler _handler;

    public ListTasksQueryHandlerTests()
    {
        _handler = new ListTasksQueryHandler(_taskRepository);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_WithNoPageOrPerPage_UsesDefaults()
    {
        var ownerId = Guid.CreateVersion7();
        _taskRepository
            .ListAsync(ownerId, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<TaskItem>(), 0));

        var query = new ListTasksQuery(ownerId, null, null, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.Paging.Page);
        Assert.Equal(20, result.Paging.PerPage);
        await _taskRepository.Received(1).ListAsync(ownerId, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_WithStatusFilter_ParsesDisplayStringToEnumBeforeCallingRepository()
    {
        var ownerId = Guid.CreateVersion7();
        _taskRepository
            .ListAsync(ownerId, DomainTaskStatus.InProgress, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<TaskItem>(), 0));

        var query = new ListTasksQuery(ownerId, "In Progress", null, null);

        await _handler.Handle(query, CancellationToken.None);

        await _taskRepository.Received(1)
            .ListAsync(ownerId, DomainTaskStatus.InProgress, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_MapsRepositoryItemsToSlimListDto()
    {
        var ownerId = Guid.CreateVersion7();
        var task = TaskItem.Create("Buy milk", "desc", null, ownerId);

        _taskRepository
            .ListAsync(ownerId, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<TaskItem> { task }, 1));

        var query = new ListTasksQuery(ownerId, null, null, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(task.Id, item.Id);
        Assert.Equal("Buy milk", item.Title);
        Assert.Equal("Pending", item.Status);
        Assert.Equal(task.DueDate, item.DueDate);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_BuildsPagingInfoWithTotalFromRepository()
    {
        var ownerId = Guid.CreateVersion7();
        _taskRepository
            .ListAsync(ownerId, null, 2, 5, Arg.Any<CancellationToken>())
            .Returns((new List<TaskItem>(), 12));

        var query = new ListTasksQuery(ownerId, null, 2, 5);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(12, result.Paging.Total);
        Assert.Equal("/api/tasks?page=1&perPage=5", result.Paging.Prev);
        Assert.Equal("/api/tasks?page=3&perPage=5", result.Paging.Next);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_WithInProgressStatus_MapsToDisplayStringWithSpaceInDto()
    {
        var ownerId = Guid.CreateVersion7();
        var task = TaskItem.Create("Task", null, null, ownerId);
        task.ChangeStatus(DomainTaskStatus.InProgress);

        _taskRepository
            .ListAsync(ownerId, DomainTaskStatus.InProgress, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new List<TaskItem> { task }, 1));

        var query = new ListTasksQuery(ownerId, "In Progress", null, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal("In Progress", result.Items[0].Status);
    }
}
