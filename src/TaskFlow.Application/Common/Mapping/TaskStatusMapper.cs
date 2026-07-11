namespace TaskFlow.Application.Common.Mapping;

/// <summary>
/// Single source of truth for translating between the <see cref="Domain.Enums.TaskStatus"/>
/// enum (C# member <c>InProgress</c>, no space) and the wire string required by
/// the API contract (<c>"In Progress"</c>, with a space). Deliberately manual —
/// <c>JsonStringEnumConverter</c> would emit <c>"InProgress"</c>, which violates
/// the contract (TASKFLOW-ANTI-DRIFT).
/// </summary>
public static class TaskStatusMapper
{
    public static string ToDisplayString(Domain.Enums.TaskStatus status) => status switch
    {
        Domain.Enums.TaskStatus.Pending => "Pending",
        Domain.Enums.TaskStatus.InProgress => "In Progress",
        Domain.Enums.TaskStatus.Completed => "Completed",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    /// <summary>
    /// Parses a wire status string into the domain enum, or <c>null</c> if the
    /// value is null or unrecognized. Callers that need to reject unrecognized
    /// values must validate BEFORE calling this (e.g. FluentValidation) — this
    /// method silently returns null for invalid input by design (it is not the
    /// validation boundary).
    /// </summary>
    public static Domain.Enums.TaskStatus? ParseOrNull(string? value) => value switch
    {
        null => null,
        "Pending" => Domain.Enums.TaskStatus.Pending,
        "In Progress" => Domain.Enums.TaskStatus.InProgress,
        "Completed" => Domain.Enums.TaskStatus.Completed,
        _ => null // validator catches invalid values before this is called
    };
}
