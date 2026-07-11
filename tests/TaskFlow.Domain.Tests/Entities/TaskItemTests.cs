using TaskFlow.Domain.Constants;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using DomainTaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Domain.Tests.Entities;

public class TaskItemTests
{
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    [Fact]
    public void Task_CreateWithValidData_SetsStatusToPendingAndAssignsId()
    {
        var task = TaskItem.Create("Buy milk", "2% milk", DateTime.UtcNow.AddDays(1), OwnerId);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(DomainTaskStatus.Pending, task.Status);
        Assert.Equal("Buy milk", task.Title);
        Assert.Equal("2% milk", task.Description);
        Assert.Equal(OwnerId, task.OwnerId);
        Assert.Equal(task.CreatedAt, task.UpdatedAt);
    }

    [Fact]
    public void Task_CreateWithEmptyTitle_ThrowsDomainException()
    {
        Assert.Throws<InvalidTaskTitleException>(
            () => TaskItem.Create(string.Empty, null, null, OwnerId));
    }

    [Fact]
    public void Task_CreateWithWhitespaceOnlyTitle_ThrowsDomainException()
    {
        Assert.Throws<InvalidTaskTitleException>(
            () => TaskItem.Create("   \t\n  ", null, null, OwnerId));
    }

    [Fact]
    public void Task_CreateWithNbspOnlyTitle_ThrowsDomainException()
    {
        Assert.Throws<InvalidTaskTitleException>(
            () => TaskItem.Create("  ", null, null, OwnerId));
    }

    [Fact]
    public void Task_CreateWithTitleExactly200Chars_Succeeds()
    {
        var title = new string('a', FieldLengths.TitleMaxLength);

        var task = TaskItem.Create(title, null, null, OwnerId);

        Assert.Equal(title, task.Title);
    }

    [Fact]
    public void Task_CreateWithTitleExceedingMaxLength_ThrowsDomainException()
    {
        var title = new string('a', FieldLengths.TitleMaxLength + 1);

        Assert.Throws<InvalidTaskTitleException>(
            () => TaskItem.Create(title, null, null, OwnerId));
    }

    [Fact]
    public void Task_CreateWithDescriptionExactly2000Chars_Succeeds()
    {
        var description = new string('b', FieldLengths.DescriptionMaxLength);

        var task = TaskItem.Create("Valid title", description, null, OwnerId);

        Assert.Equal(description, task.Description);
    }

    [Fact]
    public void Task_CreateWithDescriptionExceeding2000Chars_ThrowsDomainException()
    {
        var description = new string('b', FieldLengths.DescriptionMaxLength + 1);

        Assert.Throws<InvalidTaskTitleException>(
            () => TaskItem.Create("Valid title", description, null, OwnerId));
    }

    [Fact]
    public void Task_CreateWithDueDateExactlyEqualToNow_ThrowsDomainException()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<InvalidTaskDueDateException>(
            () => TaskItem.Create("Valid title", null, now, OwnerId));
    }

    [Fact]
    public void Task_CreateWithPastDueDate_ThrowsDomainException()
    {
        Assert.Throws<InvalidTaskDueDateException>(
            () => TaskItem.Create("Valid title", null, DateTime.UtcNow.AddDays(-1), OwnerId));
    }
}
