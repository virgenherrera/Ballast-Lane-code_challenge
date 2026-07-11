using TaskFlow.Domain.Constants;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

public sealed class TaskItem
{
    public Guid Id { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public Enums.TaskStatus Status { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid OwnerId { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private TaskItem()
    {
        Title = string.Empty;
    }

    private TaskItem(
        Guid id,
        string title,
        string? description,
        DateTime? dueDate,
        Guid ownerId,
        DateTime createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Status = Enums.TaskStatus.Pending;
        DueDate = dueDate;
        OwnerId = ownerId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static TaskItem Create(string title, string? description, DateTime? dueDate, Guid ownerId)
    {
        var trimmedTitle = title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedTitle))
        {
            throw new InvalidTaskTitleException("title required");
        }

        if (trimmedTitle.Length > FieldLengths.TitleMaxLength)
        {
            throw new InvalidTaskTitleException(
                $"title must not exceed {FieldLengths.TitleMaxLength} characters");
        }

        if (description is not null && description.Length > FieldLengths.DescriptionMaxLength)
        {
            throw new InvalidTaskTitleException(
                $"description must not exceed {FieldLengths.DescriptionMaxLength} characters");
        }

        if (dueDate.HasValue && dueDate.Value <= DateTime.UtcNow)
        {
            throw new InvalidTaskDueDateException("must be future");
        }

        return new TaskItem(
            Guid.CreateVersion7(),
            trimmedTitle,
            description,
            dueDate,
            ownerId,
            DateTime.UtcNow);
    }
}
