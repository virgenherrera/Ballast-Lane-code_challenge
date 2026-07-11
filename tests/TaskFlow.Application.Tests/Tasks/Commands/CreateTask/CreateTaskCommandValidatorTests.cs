using FluentValidation;
using FluentValidation.TestHelper;
using TaskFlow.Application.Tasks.Commands.CreateTask;
using TaskFlow.Domain.Constants;

namespace TaskFlow.Application.Tests.Tasks.Commands.CreateTask;

public class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator = new();

    public CreateTaskCommandValidatorTests()
    {
        // CascadeMode.Continue is the project-wide default configured in Program.cs
        // (EP01-B1-04a). Set it here so unit tests prove the validator shape is
        // Continue-compatible without depending on API host startup.
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue;
    }

    [Fact]
    public void CreateTaskCommandValidator_WithEmptyTitle_ReturnsValidationError()
    {
        var command = new CreateTaskCommand(string.Empty, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void CreateTaskCommandValidator_WithNbspOnlyTitle_ReturnsValidationError()
    {
        var command = new CreateTaskCommand("  ", null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void CreateTaskCommandValidator_WithPastDueDate_ReturnsValidationError()
    {
        var command = new CreateTaskCommand("Valid title", null, DateTime.UtcNow.AddDays(-1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void CreateTaskCommandValidator_WithDescriptionExceeding2000Chars_ReturnsValidationError()
    {
        var description = new string('a', FieldLengths.DescriptionMaxLength + 1);
        var command = new CreateTaskCommand("Valid title", description, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void CreateTaskCommandValidator_WithEmptyTitleAndPastDueDate_ReturnsBothErrors()
    {
        var command = new CreateTaskCommand(string.Empty, null, DateTime.UtcNow.AddDays(-1));

        var result = _validator.TestValidate(command);

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void CreateTaskCommandValidator_WithValidCommand_NoValidationErrors()
    {
        var command = new CreateTaskCommand("Valid title", "Valid description", DateTime.UtcNow.AddDays(1));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
