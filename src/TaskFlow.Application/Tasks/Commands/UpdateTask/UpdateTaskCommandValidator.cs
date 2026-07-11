using FluentValidation;
using TaskFlow.Domain.Constants;

namespace TaskFlow.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    private static readonly string[] ValidStatuses = ["Pending", "In Progress", "Completed"];

    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .Must(t => !string.IsNullOrWhiteSpace(t))
            .When(x => x.Title is not null)
            .WithMessage("title required");

        RuleFor(x => x.Title)
            .Must(t => t != null && t.Trim().Length <= FieldLengths.TitleMaxLength)
            .When(x => x.Title is not null)
            .WithMessage($"title must not exceed {FieldLengths.TitleMaxLength} characters");

        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .When(x => x.Status is not null)
            .WithMessage($"status must be one of: {string.Join(", ", ValidStatuses)}");

        RuleFor(x => x.Description)
            .Must(d => d!.Length <= FieldLengths.DescriptionMaxLength)
            .When(x => x.Description is not null)
            .WithMessage($"description must not exceed {FieldLengths.DescriptionMaxLength} characters");

        // AC-007.7: at least one field must be present
        RuleFor(x => x)
            .Must(cmd => cmd.Title is not null || cmd.Description is not null
                       || cmd.Status is not null || cmd.DueDate.HasValue)
            .WithMessage("at least one field is required")
            .WithName("payload");

        // NO dueDate past-date rejection — Update ALLOWS past dates (asymmetry with Create)
    }
}
