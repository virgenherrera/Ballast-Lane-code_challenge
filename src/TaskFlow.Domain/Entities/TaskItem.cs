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
            UtcNowTruncated());
    }

    public void Rename(string title)
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

        Title = trimmedTitle;
        UpdatedAt = UtcNowTruncated();
    }

    public void ChangeStatus(Enums.TaskStatus status)
    {
        // Free-form transition — no state machine guard (AC-007.2)
        Status = status;
        UpdatedAt = UtcNowTruncated();
    }

    public void UpdateDescription(string? description)
    {
        if (description is not null && description.Length > FieldLengths.DescriptionMaxLength)
        {
            throw new InvalidTaskTitleException(
                $"description must not exceed {FieldLengths.DescriptionMaxLength} characters");
        }

        Description = description;
        UpdatedAt = UtcNowTruncated();
    }

    public void Reschedule(DateTime? dueDate)
    {
        // CRITICAL ASYMMETRY: past dates EXPLICITLY ALLOWED here (unlike Create).
        // See TaskItemTests.Task_CreateWithPastDueDate_ThrowsDomainException for the
        // Create-side rejection this method intentionally does NOT replicate (US-007).
        DueDate = dueDate;
        UpdatedAt = UtcNowTruncated();
    }

    private const long TicksPerMicrosecond = 10;

    // PostgreSQL's `timestamp with time zone` stores microsecond precision
    // (6 fractional digits), while .NET's DateTime.UtcNow carries 100ns-tick
    // precision (7 fractional digits). Without truncation, the in-memory
    // value returned immediately after Create/Update (never round-tripped
    // through the DB) would serialize with 7-digit precision, while a
    // subsequent GET reads back the DB-truncated 6-digit value — two
    // different JSON strings for what should be the same instant. Truncating
    // at the source keeps API responses byte-for-byte consistent whether or
    // not the value has round-tripped through PostgreSQL.
    private static DateTime UtcNowTruncated()
    {
        var now = DateTime.UtcNow;
        return now.AddTicks(-(now.Ticks % TicksPerMicrosecond));
    }
}
