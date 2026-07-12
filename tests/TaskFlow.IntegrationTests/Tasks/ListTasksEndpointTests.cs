using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.IntegrationTests.Common;
using DomainTaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.IntegrationTests.Tasks;

/// <summary>
/// Full integration suite for GET /api/tasks, run against a real PostgreSQL
/// Testcontainers instance (TASKFLOW-BUILD-PIPELINE — no InMemory/SQLite
/// providers). Proves every AC-005.x and AC-009.x acceptance criterion
/// end-to-end through the real HTTP pipeline.
/// </summary>
public sealed class ListTasksEndpointTests : IntegrationTestBase
{
    private const string Endpoint = "/api/tasks";

    private static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Seeds a task directly into PostgreSQL with an explicit, deterministic
    /// <c>CreatedAt</c> (bypassing <see cref="TaskItem.Create"/>'s
    /// <c>DateTime.UtcNow</c> stamp) so ordering tests are not flaky.
    /// </summary>
    private async Task<TaskItem> SeedTaskDirectlyAsync(
        Guid ownerId,
        string title,
        DomainTaskStatus status = DomainTaskStatus.Pending,
        DateTime? createdAt = null,
        DateTime? dueDate = null)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = TaskItem.Create(title, null, dueDate, ownerId);
        task.ChangeStatus(status);
        dbContext.Tasks.Add(task);

        if (createdAt.HasValue)
        {
            dbContext.Entry(task).Property(t => t.CreatedAt).CurrentValue = createdAt.Value;
        }

        await dbContext.SaveChangesAsync();

        return task;
    }

    private sealed record ListItem(Guid Id, string Title, string Status, DateTime? DueDate);

    private sealed record Paging(int Page, int PerPage, int Total, string? Prev, string? Next);

    private sealed record ListResponse(List<ListItem> Items, Paging Paging);

    private async Task<(HttpStatusCode StatusCode, ListResponse? Body)> GetListAsync(string query = "")
    {
        var response = await AuthenticatedClient.GetAsync($"{Endpoint}{query}");
        var body = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ListResponse>(CaseInsensitive)
            : null;

        return (response.StatusCode, body);
    }

    // ---- AC-005.1 / AC-005.2 -------------------------------------------

    [Fact]
    public async Task GetList_WithNoTasks_Returns200WithEmptyItemsAndZeroTotal()
    {
        var (status, body) = await GetListAsync();

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.NotNull(body);
        Assert.Empty(body!.Items);
        Assert.Equal(0, body.Paging.Total);
    }

    [Fact]
    public async Task GetList_WithOwnedTasks_Returns200WithAllItems()
    {
        var ownerId = SeedIdentity.SeedOwnerId;
        await SeedTaskDirectlyAsync(ownerId, "Task A", createdAt: DateTime.UtcNow.AddMinutes(-3));
        await SeedTaskDirectlyAsync(ownerId, "Task B", createdAt: DateTime.UtcNow.AddMinutes(-2));
        await SeedTaskDirectlyAsync(ownerId, "Task C", createdAt: DateTime.UtcNow.AddMinutes(-1));

        var (status, body) = await GetListAsync();

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(3, body!.Items.Count);
        Assert.Equal(3, body.Paging.Total);
    }

    // ---- AC-005.3: ownership isolation ----------------------------------

    [Fact]
    public async Task GetList_WithTasksOwnedByAnotherUser_ExcludesThemFromResults()
    {
        await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId, "Mine");
        await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId2, "Not mine");

        var (_, body) = await GetListAsync();

        var item = Assert.Single(body!.Items);
        Assert.Equal("Mine", item.Title);
        Assert.Equal(1, body.Paging.Total);
    }

    // ---- AC-005.4: item shape -------------------------------------------

    [Fact]
    public async Task GetList_Item_ExposesTitleStatusAndDueDateOnly()
    {
        var dueDate = DateTime.UtcNow.AddDays(3);
        var task = await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId, "Buy milk", dueDate: dueDate);

        var (_, body) = await GetListAsync();

        var item = Assert.Single(body!.Items);
        Assert.Equal(task.Id, item.Id);
        Assert.Equal("Buy milk", item.Title);
        Assert.Equal("Pending", item.Status);
        Assert.NotNull(item.DueDate);
    }

    // ---- Ordering: createdAt DESC, id DESC -------------------------------

    [Fact]
    public async Task GetList_OrdersByCreatedAtDescending()
    {
        var ownerId = SeedIdentity.SeedOwnerId;
        var oldest = await SeedTaskDirectlyAsync(ownerId, "Oldest", createdAt: DateTime.UtcNow.AddMinutes(-10));
        var middle = await SeedTaskDirectlyAsync(ownerId, "Middle", createdAt: DateTime.UtcNow.AddMinutes(-5));
        var newest = await SeedTaskDirectlyAsync(ownerId, "Newest", createdAt: DateTime.UtcNow);

        var (_, body) = await GetListAsync();

        Assert.Equal(
            [newest.Id, middle.Id, oldest.Id],
            body!.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetList_WithSameCreatedAt_UsesIdDescendingAsTieBreaker()
    {
        var ownerId = SeedIdentity.SeedOwnerId;
        var sameInstant = DateTime.UtcNow.AddMinutes(-1);
        var first = await SeedTaskDirectlyAsync(ownerId, "First", createdAt: sameInstant);
        var second = await SeedTaskDirectlyAsync(ownerId, "Second", createdAt: sameInstant);

        var (_, body) = await GetListAsync();

        var expectedFirst = first.Id.CompareTo(second.Id) > 0 ? first.Id : second.Id;
        var expectedSecond = first.Id.CompareTo(second.Id) > 0 ? second.Id : first.Id;

        Assert.Equal([expectedFirst, expectedSecond], body!.Items.Select(i => i.Id));
    }

    // ---- AC-009.1 / AC-009.4: filter --------------------------------------

    [Fact]
    public async Task GetList_WithStatusFilter_ReturnsOnlyMatchingTasks()
    {
        var ownerId = SeedIdentity.SeedOwnerId;
        await SeedTaskDirectlyAsync(ownerId, "Pending task", DomainTaskStatus.Pending);
        await SeedTaskDirectlyAsync(ownerId, "In progress task", DomainTaskStatus.InProgress);
        await SeedTaskDirectlyAsync(ownerId, "Completed task", DomainTaskStatus.Completed);

        var (status, body) = await GetListAsync("?status=In%20Progress");

        Assert.Equal(HttpStatusCode.OK, status);
        var item = Assert.Single(body!.Items);
        Assert.Equal("In progress task", item.Title);
        Assert.Equal("In Progress", item.Status);
    }

    [Fact]
    public async Task GetList_WithNoStatusFilter_ReturnsAllStatuses()
    {
        var ownerId = SeedIdentity.SeedOwnerId;
        await SeedTaskDirectlyAsync(ownerId, "Pending task", DomainTaskStatus.Pending);
        await SeedTaskDirectlyAsync(ownerId, "In progress task", DomainTaskStatus.InProgress);
        await SeedTaskDirectlyAsync(ownerId, "Completed task", DomainTaskStatus.Completed);

        var (_, body) = await GetListAsync();

        Assert.Equal(3, body!.Items.Count);
    }

    // ---- AC-009.2: no matches ---------------------------------------------

    [Fact]
    public async Task GetList_WithStatusFilterMatchingNothing_Returns200WithEmptyItems()
    {
        await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId, "Pending task", DomainTaskStatus.Pending);

        var (status, body) = await GetListAsync("?status=Completed");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(body!.Items);
        Assert.Equal(0, body.Paging.Total);
    }

    // ---- AC-009.3: invalid filter -------------------------------------

    [Fact]
    public async Task GetList_WithInvalidStatusFilter_Returns400()
    {
        var response = await AuthenticatedClient.GetAsync($"{Endpoint}?status=Archived");

        await AssertErrorResponse.HasValidationErrorAsync(response);
    }

    // ---- Pagination -----------------------------------------------------

    [Fact]
    public async Task GetList_WithPageAndPerPage_ReturnsCorrectSlice()
    {
        var ownerId = SeedIdentity.SeedOwnerId;
        for (var i = 0; i < 12; i++)
        {
            await SeedTaskDirectlyAsync(
                ownerId, $"Task {i}", createdAt: DateTime.UtcNow.AddMinutes(-i));
        }

        var (status, body) = await GetListAsync("?page=1&perPage=5");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(5, body!.Items.Count);
        Assert.Equal(12, body.Paging.Total);
        Assert.Equal(1, body.Paging.Page);
        Assert.Equal(5, body.Paging.PerPage);
        Assert.Null(body.Paging.Prev);
        Assert.Equal("/api/tasks?page=2&perPage=5", body.Paging.Next);
    }

    [Fact]
    public async Task GetList_OnMiddlePage_ReturnsBothPrevAndNextLinks()
    {
        var ownerId = SeedIdentity.SeedOwnerId;
        for (var i = 0; i < 12; i++)
        {
            await SeedTaskDirectlyAsync(
                ownerId, $"Task {i}", createdAt: DateTime.UtcNow.AddMinutes(-i));
        }

        var (_, body) = await GetListAsync("?page=2&perPage=5");

        Assert.Equal(5, body!.Items.Count);
        Assert.Equal("/api/tasks?page=1&perPage=5", body.Paging.Prev);
        Assert.Equal("/api/tasks?page=3&perPage=5", body.Paging.Next);
    }

    [Fact]
    public async Task GetList_PageBeyondTotal_Returns200WithEmptyItemsAndCorrectTotal()
    {
        await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId, "Only task");

        var (status, body) = await GetListAsync("?page=99&perPage=10");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(body!.Items);
        Assert.Equal(1, body.Paging.Total);
    }

    [Fact]
    public async Task GetList_WithNoPageOrPerPage_UsesServerDefaults()
    {
        await SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId, "Only task");

        var (_, body) = await GetListAsync();

        Assert.Equal(1, body!.Paging.Page);
        Assert.Equal(20, body.Paging.PerPage);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=-1")]
    public async Task GetList_WithPageLessThanOne_Returns400(string query)
    {
        var response = await AuthenticatedClient.GetAsync($"{Endpoint}{query}");

        await AssertErrorResponse.HasValidationErrorAsync(response);
    }

    [Theory]
    [InlineData("?perPage=0")]
    [InlineData("?perPage=101")]
    public async Task GetList_WithPerPageOutOfRange_Returns400(string query)
    {
        var response = await AuthenticatedClient.GetAsync($"{Endpoint}{query}");

        await AssertErrorResponse.HasValidationErrorAsync(response);
    }

    // ---- Non-integer page/perPage: must not fall through to ASP.NET's
    // default ProblemDetails shape via automatic [ApiController] model
    // binding failure -------------------------------------------------------

    [Theory]
    [InlineData("?page=abc")]
    [InlineData("?page=1.5")]
    [InlineData("?perPage=abc")]
    [InlineData("?perPage=1.5")]
    public async Task GetList_WithNonIntegerParams_Returns400WithStandardShape(string queryString)
    {
        var response = await AuthenticatedClient.GetAsync($"{Endpoint}{queryString}");

        await AssertErrorResponse.HasValidationErrorAsync(response);
    }

    // ---- Filter + pagination combined ------------------------------------

    [Fact]
    public async Task GetList_WithStatusFilterAndPagination_PreservesStatusOnLinks()
    {
        var ownerId = SeedIdentity.SeedOwnerId;
        for (var i = 0; i < 8; i++)
        {
            await SeedTaskDirectlyAsync(
                ownerId, $"Pending {i}", DomainTaskStatus.Pending, DateTime.UtcNow.AddMinutes(-i));
        }

        var (_, body) = await GetListAsync("?status=Pending&page=2&perPage=5");

        Assert.Equal("/api/tasks?page=1&perPage=5&status=Pending", body!.Paging.Prev);
        Assert.Null(body.Paging.Next);
    }
}
