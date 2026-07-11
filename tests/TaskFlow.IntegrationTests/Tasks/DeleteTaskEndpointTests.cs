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
/// Full integration suite for DELETE /api/tasks/{id}, run against a real
/// PostgreSQL Testcontainers instance (TASKFLOW-BUILD-PIPELINE — no
/// InMemory/SQLite providers). Proves every AC-008.x acceptance criterion
/// end-to-end through the real HTTP pipeline.
/// </summary>
public sealed class DeleteTaskEndpointTests : IntegrationTestBase
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

    /// <summary>
    /// Direct DB row-existence check, bypassing the HTTP API (which only
    /// exposes the fixed <c>ICurrentUserContext</c> principal's own tasks).
    /// </summary>
    private async Task<bool> TaskExistsInDbAsync(Guid taskId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Tasks.AnyAsync(t => t.Id == taskId);
    }

    // ---- AC-008.1 -----------------------------------------------------

    [Fact]
    public async Task DeleteTask_WithOwnedTask_Returns204AndRemovesRecord()
    {
        var created = await CreateTaskAsync();

        var response = await Client.DeleteAsync($"{Endpoint}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var body = await response.Content.ReadAsByteArrayAsync();
        Assert.Empty(body);

        var stillExists = await TaskExistsInDbAsync(created.Id);
        Assert.False(stillExists);
    }

    // ---- AC-008.2 -----------------------------------------------------

    [Fact]
    public async Task DeleteTask_WithNonExistentId_Returns404()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await Client.DeleteAsync($"{Endpoint}/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", body.GetProperty("error").GetString());
        Assert.Equal(0, body.GetProperty("details").GetArrayLength());
    }

    // ---- AC-008.3 -----------------------------------------------------

    [Fact]
    public async Task DeleteTask_OwnedByAnotherUser_Returns404()
    {
        var otherOwnersTaskId = await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId2);

        var response = await Client.DeleteAsync($"{Endpoint}/{otherOwnersTaskId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", body.GetProperty("error").GetString());
        Assert.Equal(0, body.GetProperty("details").GetArrayLength());

        // The other owner's row must still exist — a 404 here must not have
        // silently deleted anything (ownership isolation, not just a
        // response-shape coincidence).
        var stillExists = await TaskExistsInDbAsync(otherOwnersTaskId);
        Assert.True(stillExists);
    }

    // ---- AC-008.4 -----------------------------------------------------

    [Fact]
    public async Task DeleteTask_CalledTwice_SecondCallReturns404NotServerError()
    {
        var created = await CreateTaskAsync();

        var firstResponse = await Client.DeleteAsync($"{Endpoint}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);

        var secondResponse = await Client.DeleteAsync($"{Endpoint}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, secondResponse.StatusCode);

        var body = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", body.GetProperty("error").GetString());
    }

    // ---- AC-008.5 -----------------------------------------------------

    [Fact]
    public async Task DeleteTask_SuccessResponse_HasEmptyBodyAndNoContentType()
    {
        var created = await CreateTaskAsync();

        var response = await Client.DeleteAsync($"{Endpoint}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentType);
        Assert.True(response.Content.Headers.ContentLength is null or 0);

        var bodyBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Empty(bodyBytes);
    }
}
