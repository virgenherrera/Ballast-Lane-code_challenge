using FluentValidation;

namespace TaskFlow.Application.UseCases.AuthenticateUser;

public sealed class AuthenticateUserValidator : AbstractValidator<AuthenticateUserCommand>
{
    public AuthenticateUserValidator()
    {
        CascadeMode = CascadeMode.Continue;

        // Presence only — no format/casing/strength checks here. Login validation
        // must accept ANY non-empty string so weak/legacy passwords still authenticate.
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("EMAIL_REQUIRED");

        // Presence only — deliberately NO MinimumLength, NO strength rules. A password
        // of "a" must pass THIS validator (strength was already enforced at registration
        // time; login must not re-reject legitimately weak/legacy credentials).
        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("PASSWORD_REQUIRED");
    }
}
