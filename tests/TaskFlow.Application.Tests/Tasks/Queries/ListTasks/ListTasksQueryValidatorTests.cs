using TaskFlow.Application.Tasks.Queries.ListTasks;

namespace TaskFlow.Application.Tests.Tasks.Queries.ListTasks;

public class ListTasksQueryValidatorTests
{
    private readonly ListTasksQueryValidator _validator = new();

    [Fact]
    public void Validate_WithNoOptionalFields_IsValid()
    {
        var query = new ListTasksQuery(Guid.CreateVersion7(), null, null, null);

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("In Progress")]
    [InlineData("Completed")]
    public void Validate_WithValidStatus_IsValid(string status)
    {
        var query = new ListTasksQuery(Guid.CreateVersion7(), status, null, null);

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidStatus_IsInvalid()
    {
        var query = new ListTasksQuery(Guid.CreateVersion7(), "Archived", null, null);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListTasksQuery.Status));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithPageLessThanOne_IsInvalid(int page)
    {
        var query = new ListTasksQuery(Guid.CreateVersion7(), null, page, null);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListTasksQuery.Page));
    }

    [Fact]
    public void Validate_WithPageOfOne_IsValid()
    {
        var query = new ListTasksQuery(Guid.CreateVersion7(), null, 1, null);

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithPerPageOfZero_IsInvalid()
    {
        var query = new ListTasksQuery(Guid.CreateVersion7(), null, null, 0);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListTasksQuery.PerPage));
    }

    [Fact]
    public void Validate_WithPerPageAboveMax_IsInvalid()
    {
        var query = new ListTasksQuery(Guid.CreateVersion7(), null, null, 101);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListTasksQuery.PerPage));
    }

    [Fact]
    public void Validate_WithPerPageAtMax_IsValid()
    {
        var query = new ListTasksQuery(Guid.CreateVersion7(), null, null, 100);

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithMultipleInvalidFields_CollectsAllErrorsDueToGlobalCascadeContinue()
    {
        var query = new ListTasksQuery(Guid.CreateVersion7(), "Archived", 0, 999);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListTasksQuery.Status));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListTasksQuery.Page));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListTasksQuery.PerPage));
    }
}
