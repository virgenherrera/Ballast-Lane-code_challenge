using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.API.Contracts;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.IntegrationTests.Common;

namespace TaskFlow.IntegrationTests.Tasks;

/// <summary>
/// Full integration suite for PATCH /api/tasks/{id}, run against a real
/// PostgreSQL Testcontainers instance (TASKFLOW-BUILD-PIPELINE — no
/// InMemory/SQLite providers). Proves every AC-007.x acceptance criterion
/// end-to-end through the real HTTP pipeline.
/// </summary>
public sealed class UpdateTaskEndpointTests : IntegrationTestBase
{
    private const string Endpoint = "/api/tasks";

    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private async Task<TaskResponse> CreateTaskAsync(
        string title = "Buy groceries",
        string? description = null,
        DateTime? dueDate = null)
    {
        var payload = new { title, description, dueDate };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        return body!;
    }

    /// <summary>
    /// Inserts a task directly into PostgreSQL via a scoped <see cref="AppDbContext"/>,
    /// owned by <paramref name="ownerId"/>. Used to construct state that is
    /// unreachable through the HTTP API itself (e.g. a task belonging to a
    /// different owner than the fixed <c>ICurrentUserContext</c> principal).
    /// </summary>
    private async Task<Guid> SeedTaskDirectlyAsync(Guid ownerId, string title = "Someone else's task")
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = TaskItem.Create(title, null, null, ownerId);
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();

        return task.Id;
    }

    private static HttpContent PatchBody(object payload) => JsonContent.Create(payload);

    // ---- AC-007.1 ----------------------------------------------------

    [Fact]
    public async Task UpdateTask_WithNewTitle_Returns200WithTitleUpdated()
    {
        var created = await CreateTaskAsync();

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { title = "Buy groceries and milk" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal("Buy groceries and milk", body!.Title);
    }

    // ---- AC-007.2 ----------------------------------------------------

    // Input status string (accepted by the validator/ParseStatus switch) ->
    // expected serialized output, which MUST go through TaskStatusMapper.
    // ToDisplayString and therefore round-trips "In Progress" WITH the space
    // — this is the real API contract (TASKFLOW-ANTI-DRIFT), distinct from
    // the enum member's own ToString() ("InProgress", no space).
    [Theory]
    [InlineData("In Progress", "In Progress")]
    [InlineData("Completed", "Completed")]
    [InlineData("Pending", "Pending")]
    public async Task UpdateTask_WithValidStatusTransitionAnyDirection_Returns200WithNewStatus(
        string inputStatus, string expectedOutputStatus)
    {
        var created = await CreateTaskAsync();

        // Drive to Completed first so the Pending case below is a genuine
        // Completed -> Pending "backwards" transition, proving there is no
        // state-machine guard (free-form transitions per AC-007.2).
        if (inputStatus == "Pending")
        {
            var toCompleted = await AuthenticatedClient.PatchAsync(
                $"{Endpoint}/{created.Id}",
                PatchBody(new { status = "Completed" }));
            Assert.Equal(HttpStatusCode.OK, toCompleted.StatusCode);
        }

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { status = inputStatus }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal(expectedOutputStatus, body!.Status);
    }

    // ---- AC-007.3 (critical asymmetry) -------------------------------

    [Fact]
    public async Task UpdateTask_WithPastDueDate_Returns200()
    {
        var created = await CreateTaskAsync();
        var pastDate = DateTime.UtcNow.AddDays(-1);

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { dueDate = pastDate }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.NotNull(body!.DueDate);
        Assert.True(body.DueDate <= DateTime.UtcNow);
    }

    /// <summary>
    /// Companion asymmetry proof, kept in the same file as its Update
    /// counterpart above: Create still REJECTS past due dates. Together,
    /// these two tests prove the asymmetry is real and intentional, not an
    /// oversight in either handler.
    /// </summary>
    [Fact]
    public async Task CreateTask_WithPastDueDate_StillReturns400()
    {
        var payload = new
        {
            title = "Buy groceries",
            description = (string?)null,
            dueDate = DateTime.UtcNow.AddDays(-1),
        };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        await AssertErrorResponse.HasValidationErrorAsync(
            response,
            ("dueDate", "must be future"));
    }

    // ---- AC-007.4 ----------------------------------------------------

    [Fact]
    public async Task UpdateTask_WithInvalidStatusEnumValue_Returns400()
    {
        var created = await CreateTaskAsync();

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { status = "NotARealStatus" }));

        await AssertErrorResponse.HasValidationErrorAsync(
            response,
            ("status", "status must be one of: Pending, In Progress, Completed"));
    }

    // ---- AC-007.5 -----------------------------------------------------

    [Fact]
    public async Task UpdateTask_OwnedByAnotherUser_Returns404()
    {
        var otherOwnersTaskId = await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId2);

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{otherOwnersTaskId}",
            PatchBody(new { title = "Hostile takeover" }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_NonExistentId_Returns404IdenticalToOwnerMismatch()
    {
        var otherOwnersTaskId = await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId2);
        var patchPayload = new { title = "Doesn't matter" };

        var nonExistentResponse = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{Guid.NewGuid()}",
            PatchBody(patchPayload));
        var mismatchResponse = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{otherOwnersTaskId}",
            PatchBody(patchPayload));

        Assert.Equal(HttpStatusCode.NotFound, nonExistentResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, mismatchResponse.StatusCode);

        var nonExistentBody = await nonExistentResponse.Content.ReadFromJsonAsync<JsonElement>();
        var mismatchBody = await mismatchResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Structural equality: the two 404 payloads must be indistinguishable
        // to the caller. A caller must not be able to tell "task doesn't
        // exist" apart from "task exists but belongs to someone else" —
        // that distinction would itself leak the existence of other users'
        // tasks. Compare the raw serialized JSON of each (the handler emits
        // a fixed static body with no per-request dynamic fields such as
        // the requested task id).
        Assert.Equal(nonExistentBody.GetRawText(), mismatchBody.GetRawText());

        Assert.Equal(404, nonExistentBody.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", nonExistentBody.GetProperty("error").GetString());
        Assert.Equal("The requested task was not found.", nonExistentBody.GetProperty("message").GetString());
        Assert.Equal(0, nonExistentBody.GetProperty("details").GetArrayLength());
    }

    // ---- AC-007.6 ------------------------------------------------------

    [Fact]
    public async Task UpdateTask_WithEmptyTitleString_Returns400()
    {
        var created = await CreateTaskAsync();

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { title = "" }));

        await AssertErrorResponse.HasValidationErrorAsync(
            response,
            ("title", "title required"));
    }

    [Fact]
    public async Task UpdateTask_WithWhitespaceOnlyTitle_Returns400()
    {
        var created = await CreateTaskAsync();

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { title = "   " }));

        await AssertErrorResponse.HasValidationErrorAsync(
            response,
            ("title", "title required"));
    }

    // ---- AC-007.7 -----------------------------------------------------

    [Fact]
    public async Task UpdateTask_WithEmptyPayload_Returns400RequiresAtLeastOneField()
    {
        var created = await CreateTaskAsync();

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { }));

        await AssertErrorResponse.HasValidationErrorAsync(
            response,
            ("payload", "at least one field is required"));
    }

    // ---- Partial update preserves untouched fields ---------------------

    [Fact]
    public async Task UpdateTask_WithPartialValidPayload_Returns200WithUpdatedFields()
    {
        var dueDate = DateTime.UtcNow.AddDays(10);
        var created = await CreateTaskAsync(
            title: "Original title",
            description: "Original description",
            dueDate: dueDate);

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { title = "New title only" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal("New title only", body!.Title);

        // Untouched fields retain their pre-update values.
        Assert.Equal("Original description", body.Description);
        Assert.Equal("Pending", body.Status);
        Assert.NotNull(body.DueDate);
        Assert.Equal(dueDate, body.DueDate!.Value, TimeSpan.FromSeconds(1));
    }

    // ---- AC-007.8 -----------------------------------------------------

    // NOTE: this 400 is hand-built directly in TasksController.Update for the
    // route-parsing failure path — it never reaches FluentValidation, so its
    // "message" is "Task id is not a valid GUID.", not the generic
    // "One or more validation errors occurred." produced by
    // ValidationExceptionHandler. AssertErrorResponse.HasValidationErrorAsync
    // hardcodes the latter, so it cannot be reused here; asserted directly
    // instead against the same standard envelope shape (status/error/message/details).
    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    public async Task UpdateTask_WithMalformedGuidInRoute_Returns400(string malformedId)
    {
        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{malformedId}",
            PatchBody(new { title = "Doesn't matter" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("error").GetString());
        Assert.Equal("Task id is not a valid GUID.", body.GetProperty("message").GetString());

        var details = body.GetProperty("details");
        Assert.Equal(1, details.GetArrayLength());
        var detail = details[0];
        Assert.Equal("id", detail.GetProperty("field").GetString());
        Assert.Equal("must be a valid UUID/GUID", detail.GetProperty("issue").GetString());
    }

    // ---- updatedAt monotonicity -----------------------------------------

    [Fact]
    public async Task UpdateTask_ChangeStatus_UpdatedAtIsRefreshedToLaterTimestamp()
    {
        var created = await CreateTaskAsync();

        // Guarantee a measurable timestamp delta regardless of clock resolution.
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { status = "Completed" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.True(body!.UpdatedAt > created.UpdatedAt);
        Assert.Equal(created.CreatedAt, body.CreatedAt);
    }

    // ---- Full response shape --------------------------------------------

    [Fact]
    public async Task UpdateTask_WithValidPayload_Returns200WithExactExpectedFieldSet()
    {
        var created = await CreateTaskAsync();

        var response = await AuthenticatedClient.PatchAsync(
            $"{Endpoint}/{created.Id}",
            PatchBody(new { title = "Updated title", status = "In Progress" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var expectedProperties = new[]
        {
            "id", "title", "description", "status", "dueDate", "ownerId", "createdAt", "updatedAt",
        };

        var actualProperties = root.EnumerateObject().Select(p => p.Name).ToArray();

        Assert.Equal(expectedProperties.Length, actualProperties.Length);
        foreach (var expected in expectedProperties)
        {
            Assert.Contains(expected, actualProperties);
        }

        Assert.Equal(created.Id, root.GetProperty("id").GetGuid());
        Assert.Equal("Updated title", root.GetProperty("title").GetString());
        Assert.Equal("In Progress", root.GetProperty("status").GetString());
        Assert.Equal(SeedIdentity.SeedOwnerId, root.GetProperty("ownerId").GetGuid());
    }
}
