using FluentValidation;
using TaskFlow.Application.Common.Pagination;

namespace TaskFlow.Application.Tasks.Queries.ListTasks;

/// <summary>
/// Validates the raw <see cref="ListTasksQuery"/> wire values: <c>page</c>
/// must be &gt;= 1, <c>perPage</c> must be within 1..<see cref="PaginationDefaults.MaxPerPage"/>,
/// and <c>status</c> (if provided) must be one of the three exact enum
/// display strings. Does NOT set <c>CascadeMode</c> — the global default is
/// configured once in Program.cs (TASKFLOW-ANTI-DRIFT).
/// </summary>
public sealed class ListTasksQueryValidator : AbstractValidator<ListTasksQuery>
{
    private static readonly string[] ValidStatuses = ["Pending", "In Progress", "Completed"];

    public ListTasksQueryValidator()
    {
        RuleFor(x => x.Page)
            .Must(p => p!.Value >= 1)
            .When(x => x.Page.HasValue)
            .WithMessage("page must be >= 1");

        // Upper bound prevents (page - 1) * perPage from overflowing Int32
        // (perPage is capped at PaginationDefaults.MaxPerPage) and producing
        // a negative Skip argument -> ArgumentOutOfRangeException -> 500.
        RuleFor(x => x.Page)
            .LessThanOrEqualTo(int.MaxValue / PaginationDefaults.MaxPerPage)
            .When(x => x.Page.HasValue)
            .WithMessage("page value is too large");

        RuleFor(x => x.PerPage)
            .Must(p => p!.Value is >= 1 and <= PaginationDefaults.MaxPerPage)
            .When(x => x.PerPage.HasValue)
            .WithMessage($"perPage must be between 1 and {PaginationDefaults.MaxPerPage}");

        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .When(x => x.Status is not null)
            .WithMessage($"status must be one of: {string.Join(", ", ValidStatuses)}");
    }
}
