using FluentValidation;

namespace TaskFlow.Application.UseCases.RegisterUser;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        CascadeMode = CascadeMode.Continue;

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("EMAIL_REQUIRED");

        RuleFor(x => x.Email)
            .Must(e => e == e.ToLowerInvariant()).WithErrorCode("EMAIL_UPPERCASE")
            .EmailAddress().WithErrorCode("EMAIL_INVALID_FORMAT")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("NAME_REQUIRED");

        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithErrorCode("NAME_WHITESPACE_ONLY")
            .MaximumLength(100).WithErrorCode("NAME_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("PASSWORD_REQUIRED");

        RuleFor(x => x.Password)
            .MinimumLength(8).WithErrorCode("PASSWORD_TOO_SHORT")
            .MaximumLength(72).WithErrorCode("PASSWORD_TOO_LONG")
            .Must(p => p.Any(char.IsUpper)).WithErrorCode("PASSWORD_MISSING_UPPERCASE")
            .Must(p => p.Any(char.IsDigit)).WithErrorCode("PASSWORD_MISSING_DIGIT")
            .Must(p => p.Any(c => !char.IsLetterOrDigit(c))).WithErrorCode("PASSWORD_MISSING_SPECIAL")
            .When(x => !string.IsNullOrEmpty(x.Password));
        // NOTE: each .Must() is an INDEPENDENT rule link on its RuleFor chain — with
        // CascadeMode.Continue, FluentValidation still evaluates all of them and reports
        // one ValidationFailure per broken rule (AC-001.3 requires this, not fail-fast).
        // NotEmpty() is split into its own RuleFor (ungated) so an empty string still
        // reports *_REQUIRED without also cascading into misleading compound errors from
        // the dependent .Must() checks — .When() applies to the whole chain it's attached
        // to, so it must not sit on the same chain as NotEmpty().
    }
}
