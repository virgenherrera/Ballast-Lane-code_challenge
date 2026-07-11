using FluentValidation;
using TaskFlow.Domain.Constants;

namespace TaskFlow.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        // Do NOT set CascadeMode here — global default set in Program.cs (EP01-B1-04a)
        RuleFor(x => x.Title)
            .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("title required")
            .Must(t => t != null && t.Trim().Length <= FieldLengths.TitleMaxLength)
            .WithMessage("title must not exceed 200 characters");
        RuleFor(x => x.DueDate)
            .Must(d => !d.HasValue || d.Value > DateTime.UtcNow)
            .When(x => x.DueDate.HasValue)
            .WithMessage("must be future");
        RuleFor(x => x.Description)
            .Must(d => d == null || d.Length <= FieldLengths.DescriptionMaxLength)
            .WithMessage("description must not exceed 2000 characters");
    }
}
