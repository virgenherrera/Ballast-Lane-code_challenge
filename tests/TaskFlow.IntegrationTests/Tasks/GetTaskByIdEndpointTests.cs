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
/// Full integration suite for GET /api/tasks/{id}, run against a real
/// PostgreSQL Testcontainers instance (TASKFLOW-BUILD-PIPELINE — no
/// InMemory/SQLite providers). Proves every AC-006.x acceptance criterion
/// end-to-end through the real HTTP pipeline, including byte-identical 404
/// parity between "not found" and "owned by another user" (anti-enumeration).
/// </summary>
public sealed class GetTaskByIdEndpointTests : IntegrationTestBase
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

        var response = await Client.PostAsJsonAsync(Endpoint, payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        return body!;
    }

    private async Task<Guid> SeedTaskDirectlyAsync(Guid ownerId, string title = "Someone else's task")
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = TaskItem.Create(title, null, null, ownerId);
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();

        return task.Id;
    }

    // ---- AC-006.1: full detail shape --------------------------------------

    [Fact]
    public async Task GetById_ForOwnedTask_Returns200WithFullEightFieldShape()
    {
        var created = await CreateTaskAsync("Buy milk", "2% please", DateTime.UtcNow.AddDays(2));

        var response = await Client.GetAsync($"{Endpoint}/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.NotNull(body);
        Assert.Equal(created.Id, body!.Id);
        Assert.Equal("Buy milk", body.Title);
        Assert.Equal("2% please", body.Description);
        Assert.Equal("Pending", body.Status);
        Assert.NotNull(body.DueDate);
        Assert.Equal(SeedIdentity.SeedOwnerId, body.OwnerId);
        Assert.NotEqual(default, body.CreatedAt);
        Assert.NotEqual(default, body.UpdatedAt);
    }

    // ---- AC-006.2: not found -----------------------------------------------

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await Client.GetAsync($"{Endpoint}/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", body.GetProperty("error").GetString());
        Assert.Equal(0, body.GetProperty("details").GetArrayLength());
    }

    // ---- AC-006.3: cross-owner masked as not-found -------------------------

    [Fact]
    public async Task GetById_OwnedByAnotherUser_Returns404NotForbidden()
    {
        var otherOwnersTaskId = await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId2);

        var response = await Client.GetAsync($"{Endpoint}/{otherOwnersTaskId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", body.GetProperty("error").GetString());
        Assert.Equal(0, body.GetProperty("details").GetArrayLength());
    }

    [Fact]
    public async Task GetById_NotFoundAndCrossOwner_ProduceByteIdenticalResponseBodies()
    {
        var otherOwnersTaskId = await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId2);
        var nonExistentId = Guid.NewGuid();

        var crossOwnerResponse = await Client.GetAsync($"{Endpoint}/{otherOwnersTaskId}");
        var notFoundResponse = await Client.GetAsync($"{Endpoint}/{nonExistentId}");

        var crossOwnerBody = await crossOwnerResponse.Content.ReadAsStringAsync();
        var notFoundBody = await notFoundResponse.Content.ReadAsStringAsync();

        Assert.Equal(crossOwnerResponse.StatusCode, notFoundResponse.StatusCode);
        Assert.Equal(notFoundBody, crossOwnerBody);
    }

    // ---- Malformed GUID ------------------------------------------------

    [Fact]
    public async Task GetById_WithMalformedGuid_Returns400NotRoutingLevel404()
    {
        var response = await Client.GetAsync($"{Endpoint}/not-a-guid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("error").GetString());
        Assert.Equal("Task id is not a valid GUID.", body.GetProperty("message").GetString());
    }

    // ---- Description null handling ------------------------------------

    [Fact]
    public async Task GetById_ForTaskWithoutDescription_ReturnsNullDescription()
    {
        var created = await CreateTaskAsync("No description task");

        var response = await Client.GetAsync($"{Endpoint}/{created.Id}");

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.Null(body!.Description);
    }

    // ---- TaskStatusMapper: InProgress -> "In Progress" (with space) -------

    [Fact]
    public async Task GetById_ForTaskWithInProgressStatus_ReturnsStatusWithSpace()
    {
        var created = await CreateTaskAsync("Task in progress");

        var patchResponse = await Client.PatchAsync(
            $"{Endpoint}/{created.Id}",
            JsonContent.Create(new { status = "In Progress" }));
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var response = await Client.GetAsync($"{Endpoint}/{created.Id}");

        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(CaseInsensitive);
        Assert.Equal("In Progress", body!.Status);
    }

    // ---- Deleted task is not found --------------------------------------

    [Fact]
    public async Task GetById_ForDeletedTask_Returns404()
    {
        var created = await CreateTaskAsync();

        var deleteResponse = await Client.DeleteAsync($"{Endpoint}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var response = await Client.GetAsync($"{Endpoint}/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Cross-owner task is invisible but still exists in DB ------------

    [Fact]
    public async Task GetById_OwnedByAnotherUser_DoesNotDeleteOrMutateTheOtherUsersRow()
    {
        var otherOwnersTaskId = await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId2, "Untouchable");

        var response = await Client.GetAsync($"{Endpoint}/{otherOwnersTaskId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stillExists = await dbContext.Tasks.AnyAsync(t => t.Id == otherOwnersTaskId);
        Assert.True(stillExists);
    }
}
