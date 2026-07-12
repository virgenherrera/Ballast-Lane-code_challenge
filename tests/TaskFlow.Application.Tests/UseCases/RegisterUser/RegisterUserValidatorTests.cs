using FluentValidation;
using FluentValidation.TestHelper;
using TaskFlow.Application.UseCases.RegisterUser;

namespace TaskFlow.Application.Tests.UseCases.RegisterUser;

public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();

    public RegisterUserValidatorTests()
    {
        // CascadeMode.Continue is the project-wide default configured in Program.cs
        // (EP01-B1-04a). Set it here so unit tests prove the validator shape is
        // Continue-compatible without depending on API host startup.
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue;
    }

    private static RegisterUserCommand ValidCommand(
        string email = "jane@example.com",
        string name = "Jane Doe",
        string password = "Str0ng!Pass") => new(email, name, password);

    [Fact]
    public void Validate_PasswordTooShort_FailsWithLengthError()
    {
        var command = ValidCommand(password: "Sh0rt!a");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("PASSWORD_TOO_SHORT");
    }

    [Fact]
    public void Validate_PasswordExactlyMinLength_Passes()
    {
        var command = ValidCommand(password: "Str0ng!8");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordExceedsBcryptLimit_Fails()
    {
        var password = "Str0ng!" + new string('a', 70);
        var command = ValidCommand(password: password);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("PASSWORD_TOO_LONG");
    }

    [Fact]
    public void Validate_PasswordMissingUppercase_Fails()
    {
        var command = ValidCommand(password: "str0ng!pass");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("PASSWORD_MISSING_UPPERCASE");
    }

    [Fact]
    public void Validate_PasswordMissingDigit_Fails()
    {
        var command = ValidCommand(password: "Strong!Pass");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("PASSWORD_MISSING_DIGIT");
    }

    [Fact]
    public void Validate_PasswordMissingSpecialChar_Fails()
    {
        var command = ValidCommand(password: "Str0ngPass");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("PASSWORD_MISSING_SPECIAL");
    }

    [Fact]
    public void Validate_PasswordMeetsAllRules_Passes()
    {
        var command = ValidCommand(password: "Str0ng!Pass");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_EmptyEmail_FailsNamingEmailField()
    {
        var command = ValidCommand(email: string.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorCode("EMAIL_REQUIRED");
    }

    [Fact]
    public void Validate_EmptyName_FailsNamingNameField()
    {
        var command = ValidCommand(name: string.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode("NAME_REQUIRED");
    }

    [Fact]
    public void Validate_EmptyPassword_FailsNamingPasswordField()
    {
        var command = ValidCommand(password: string.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("PASSWORD_REQUIRED");
    }

    [Fact]
    public void Validate_AllFieldsMissing_ReturnsAllThreeErrors()
    {
        var command = new RegisterUserCommand(string.Empty, string.Empty, string.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Password);
        Assert.True(result.Errors.Count >= 3);
    }

    [Fact]
    public void Validate_UppercaseEmail_FailsValidation()
    {
        var command = ValidCommand(email: "Jane@Example.com");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorCode("EMAIL_UPPERCASE");
    }

    [Fact]
    public void Validate_NameWhitespaceOnly_FailsValidation()
    {
        var command = ValidCommand(name: "   ");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode("NAME_WHITESPACE_ONLY");
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_FailsValidation()
    {
        var command = ValidCommand(name: new string('a', 101));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode("NAME_TOO_LONG");
    }
}
