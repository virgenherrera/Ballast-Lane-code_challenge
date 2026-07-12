using FluentValidation;
using FluentValidation.TestHelper;
using TaskFlow.Application.UseCases.AuthenticateUser;

namespace TaskFlow.Application.Tests.UseCases.AuthenticateUser;

public class AuthenticateUserValidatorTests
{
    private readonly AuthenticateUserValidator _validator = new();

    public AuthenticateUserValidatorTests()
    {
        // CascadeMode.Continue is the project-wide default configured in Program.cs
        // (EP01-B1-04a). Set it here so unit tests prove the validator shape is
        // Continue-compatible without depending on API host startup.
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue;
    }

    [Fact]
    public void Validate_EmptyEmail_FailsNamingEmailField()
    {
        var command = new AuthenticateUserCommand(string.Empty, "any-password");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_EmptyPassword_FailsNamingPasswordField()
    {
        var command = new AuthenticateUserCommand("user@example.com", string.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_BothFieldsEmpty_ReturnsTwoDistinctErrors()
    {
        var command = new AuthenticateUserCommand(string.Empty, string.Empty);

        var result = _validator.TestValidate(command);

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Validate_WeakPassword_DoesNotFailStrengthRules()
    {
        var command = new AuthenticateUserCommand("user@example.com", "a");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
