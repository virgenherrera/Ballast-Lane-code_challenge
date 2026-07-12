using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.API.Contracts;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.IntegrationTests.Common;

namespace TaskFlow.IntegrationTests.Tasks;

/// <summary>
/// Full integration suite for POST /api/tasks, run against a real
/// PostgreSQL Testcontainers instance (TASKFLOW-BUILD-PIPELINE — no
/// InMemory/SQLite providers). Proves every AC-004.x acceptance criterion
/// end-to-end through the real HTTP pipeline.
/// </summary>
public sealed class CreateTaskTests : IntegrationTestBase
{
    private const string Endpoint = "/api/tasks";

    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task CreateTask_WithEmptyTitle_Returns400()
    {
        var payload = new { title = "", description = (string?)null, dueDate = (DateTime?)null };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        await AssertErrorResponse.HasValidationErrorAsync(
            response,
            ("title", "title required"));
    }

    [Fact]
    public async Task CreateTask_WithWhitespaceOnlyTitle_Returns400()
    {
        var payload = new { title = "   ", description = (string?)null, dueDate = (DateTime?)null };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        await AssertErrorResponse.HasValidationErrorAsync(
            response,
            ("title", "title required"));
    }

    [Fact]
    public async Task CreateTask_WithTitleExceeding200Chars_Returns400()
    {
        var payload = new
        {
            title = new string('a', 201),
            description = (string?)null,
            dueDate = (DateTime?)null,
        };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        await AssertErrorResponse.HasValidationErrorAsync(
            response,
            ("title", "title must not exceed 200 characters"));
    }

    [Fact]
    public async Task CreateTask_WithTitleExactly200Chars_Returns201()
    {
        var title = new string('a', 200);
        var payload = new { title, description = (string?)null, dueDate = (DateTime?)null };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal(title, body!.Title);
    }

    [Fact]
    public async Task CreateTask_WithPastDueDate_Returns400()
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

    [Fact]
    public async Task CreateTask_WithDueDateExactlyEqualToNow_Returns400()
    {
        // Exclusive boundary: dueDate must be strictly greater than "now".
        // A value equal to the instant the request is validated must fail.
        var payload = new
        {
            title = "Buy groceries",
            description = (string?)null,
            dueDate = DateTime.UtcNow,
        };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        await AssertErrorResponse.HasValidationErrorAsync(
            response,
            ("dueDate", "must be future"));
    }

    [Fact]
    public async Task CreateTask_WithStatusOmittedInBody_DefaultsToPending()
    {
        var payload = new { title = "Buy groceries" };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal("Pending", body!.Status);
    }

    [Fact]
    public async Task CreateTask_WithStatusSuppliedInBody_IgnoresValueAndDefaultsToPending()
    {
        // CreateTaskRequest has no Status property, so a client-supplied
        // "status" in the raw JSON body is silently ignored (System.Text.Json
        // default UnmappedMemberHandling = Skip) — the task is still created
        // as Pending.
        var payload = new { title = "Buy groceries", status = "Completed" };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal("Pending", body!.Status);
    }

    [Fact]
    public async Task CreateTask_WithValidPayload_Returns201WithOwnerIdSet()
    {
        var payload = new { title = "Buy groceries" };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal(SeedIdentity.SeedOwnerId, body!.OwnerId);
    }

    [Fact]
    public async Task CreateTask_WithTitleOnly_Returns201WithNullableFieldsNull()
    {
        var payload = new { title = "Buy groceries" };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Null(body!.Description);
        Assert.Null(body.DueDate);
    }

    [Fact]
    public async Task CreateTask_WithMultipleInvalidFields_Returns400WithAllDetails()
    {
        // Proves CascadeMode.Continue end-to-end: both the title rule and
        // the description rule fail, and BOTH must be reported (not just
        // the first one encountered).
        var payload = new
        {
            title = "",
            description = new string('a', 2001),
            dueDate = (DateTime?)null,
        };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        var body = await AssertErrorResponse.HasValidationErrorAsync(response);

        Assert.Equal(2, body.Details.Count);
    }

    [Fact]
    public async Task CreateTask_WithClientSuppliedIdAndOwnerId_IgnoresClientValues()
    {
        var clientId = Guid.NewGuid();
        var clientOwnerId = Guid.NewGuid();

        var payload = new
        {
            id = clientId,
            ownerId = clientOwnerId,
            title = "Buy groceries",
        };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.NotEqual(clientId, body!.Id);
        Assert.NotEqual(clientOwnerId, body.OwnerId);
        Assert.Equal(SeedIdentity.SeedOwnerId, body.OwnerId);
    }

    [Fact]
    public async Task CreateTask_WithUnknownExtraJsonProperty_Returns201NotError()
    {
        var payload = new
        {
            title = "Buy groceries",
            someTotallyUnknownField = "should be ignored",
        };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithValidPayload_Returns201WithExactExpectedFieldSet()
    {
        var dueDate = DateTime.UtcNow.AddDays(7);
        var payload = new
        {
            title = "Buy groceries",
            description = "Milk, eggs, bread",
            dueDate,
        };

        var response = await AuthenticatedClient.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

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

        Assert.Equal("Buy groceries", root.GetProperty("title").GetString());
        Assert.Equal("Milk, eggs, bread", root.GetProperty("description").GetString());
        Assert.Equal("Pending", root.GetProperty("status").GetString());
        Assert.Equal(SeedIdentity.SeedOwnerId, root.GetProperty("ownerId").GetGuid());
        Assert.NotEqual(Guid.Empty, root.GetProperty("id").GetGuid());
    }
}
