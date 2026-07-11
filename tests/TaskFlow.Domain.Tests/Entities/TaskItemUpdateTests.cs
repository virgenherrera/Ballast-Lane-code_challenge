using TaskFlow.Domain.Constants;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using DomainTaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Domain.Tests.Entities;

public class TaskItemUpdateTests
{
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private static TaskItem CreateTask(
        string title = "Original title",
        string? description = "Original description",
        DateTime? dueDate = null,
        Enums.TaskStatus? status = null)
    {
        var task = TaskItem.Create(title, description, dueDate ?? DateTime.UtcNow.AddDays(1), OwnerId);

        if (status.HasValue)
        {
            task.ChangeStatus(status.Value);
        }

        return task;
    }

    [Fact]
    public void TaskItem_Rename_WithNonEmptyTitle_UpdatesTitleAndUpdatedAt()
    {
        var task = CreateTask();
        var previousUpdatedAt = task.UpdatedAt;

        task.Rename("New title");

        Assert.Equal("New title", task.Title);
        Assert.True(task.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void TaskItem_Rename_WithEmptyTitle_ThrowsDomainException()
    {
        var task = CreateTask();

        Assert.Throws<InvalidTaskTitleException>(() => task.Rename(string.Empty));
    }

    [Fact]
    public void TaskItem_Rename_WithWhitespaceOnlyTitle_ThrowsDomainException()
    {
        var task = CreateTask();

        Assert.Throws<InvalidTaskTitleException>(() => task.Rename("   \t\n  "));
    }

    [Fact]
    public void TaskItem_Rename_WithTitleExceedingMaxLength_ThrowsDomainException()
    {
        var task = CreateTask();
        var title = new string('a', FieldLengths.TitleMaxLength + 1);

        Assert.Throws<InvalidTaskTitleException>(() => task.Rename(title));
    }

    [Theory]
    [InlineData(DomainTaskStatus.Pending, DomainTaskStatus.Pending)]
    [InlineData(DomainTaskStatus.Pending, DomainTaskStatus.InProgress)]
    [InlineData(DomainTaskStatus.Pending, DomainTaskStatus.Completed)]
    [InlineData(DomainTaskStatus.InProgress, DomainTaskStatus.Pending)]
    [InlineData(DomainTaskStatus.InProgress, DomainTaskStatus.InProgress)]
    [InlineData(DomainTaskStatus.InProgress, DomainTaskStatus.Completed)]
    [InlineData(DomainTaskStatus.Completed, DomainTaskStatus.Pending)]
    [InlineData(DomainTaskStatus.Completed, DomainTaskStatus.InProgress)]
    [InlineData(DomainTaskStatus.Completed, DomainTaskStatus.Completed)]
    public void TaskItem_ChangeStatus_ToAnyValidValueFromAnyCurrentStatus_UpdatesStatus(
        DomainTaskStatus from,
        DomainTaskStatus to)
    {
        var task = CreateTask(status: from);
        var previousUpdatedAt = task.UpdatedAt;

        task.ChangeStatus(to);

        Assert.Equal(to, task.Status);
        Assert.True(task.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void TaskItem_UpdateDescription_WithValue_UpdatesDescriptionAndUpdatedAt()
    {
        var task = CreateTask();
        var previousUpdatedAt = task.UpdatedAt;

        task.UpdateDescription("Updated description");

        Assert.Equal("Updated description", task.Description);
        Assert.True(task.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void TaskItem_UpdateDescription_WithNull_ClearsDescriptionAndUpdatesUpdatedAt()
    {
        var task = CreateTask(description: "Original description");
        var previousUpdatedAt = task.UpdatedAt;

        task.UpdateDescription(null);

        Assert.Null(task.Description);
        Assert.True(task.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void TaskItem_UpdateDescription_ExceedingMaxLength_ThrowsDomainException()
    {
        var task = CreateTask();
        var description = new string('b', FieldLengths.DescriptionMaxLength + 1);

        Assert.Throws<InvalidTaskTitleException>(() => task.UpdateDescription(description));
    }

    [Fact]
    public void TaskItem_Reschedule_WithFutureDate_UpdatesDueDateAndUpdatedAt()
    {
        var task = CreateTask();
        var previousUpdatedAt = task.UpdatedAt;
        var futureDate = DateTime.UtcNow.AddDays(5);

        task.Reschedule(futureDate);

        Assert.Equal(futureDate, task.DueDate);
        Assert.True(task.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void TaskItem_Reschedule_WithPastDate_DoesNotThrow()
    {
        // CRITICAL ASYMMETRY: unlike TaskItemTests.Task_CreateWithPastDueDate_ThrowsDomainException,
        // Reschedule MUST accept past dates without throwing (US-007). This is intentional and
        // must never be "fixed" to mirror Create()'s validation.
        var task = CreateTask();
        var pastDate = DateTime.UtcNow.AddDays(-1);

        var exception = Record.Exception(() => task.Reschedule(pastDate));

        Assert.Null(exception);
        Assert.Equal(pastDate, task.DueDate);
    }

    [Fact]
    public void TaskItem_Reschedule_WithNull_ClearsDueDateAndUpdatesUpdatedAt()
    {
        var task = CreateTask(dueDate: DateTime.UtcNow.AddDays(1));
        var previousUpdatedAt = task.UpdatedAt;

        task.Reschedule(null);

        Assert.Null(task.DueDate);
        Assert.True(task.UpdatedAt >= previousUpdatedAt);
    }
}
