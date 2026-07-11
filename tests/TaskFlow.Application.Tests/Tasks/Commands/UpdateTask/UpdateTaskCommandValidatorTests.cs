using FluentValidation;
using FluentValidation.TestHelper;
using TaskFlow.Application.Tasks.Commands.UpdateTask;

namespace TaskFlow.Application.Tests.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandValidatorTests
{
    private readonly UpdateTaskCommandValidator _validator = new();

    public UpdateTaskCommandValidatorTests()
    {
        // CascadeMode.Continue is the project-wide default configured in Program.cs
        // (EP01-B1-04a). Set it here so unit tests prove the validator shape is
        // Continue-compatible without depending on API host startup.
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue;
    }

    [Fact]
    public void UpdateTaskCommandValidator_WithInvalidStatusString_FailsWithValidValuesListed()
    {
        var command = new UpdateTaskCommand(Guid.CreateVersion7(), null, null, "Bogus", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorMessage("status must be one of: Pending, In Progress, Completed");
    }

    [Fact]
    public void UpdateTaskCommandValidator_WithEmptyTitleAndInvalidStatus_FailsWithBothErrorsInDetails()
    {
        var command = new UpdateTaskCommand(Guid.CreateVersion7(), string.Empty, null, "Bogus", null);

        var result = _validator.TestValidate(command);

        Assert.True(result.Errors.Count >= 2);
    }

    [Fact]
    public void UpdateTaskCommandValidator_WithNoFieldsProvided_Fails()
    {
        var command = new UpdateTaskCommand(Guid.CreateVersion7(), null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("payload");
    }

    [Fact]
    public void UpdateTaskCommandValidator_WithPastDueDate_DoesNotFail()
    {
        var command = new UpdateTaskCommand(
            Guid.CreateVersion7(), null, null, null, DateTime.UtcNow.AddDays(-10));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }
}
