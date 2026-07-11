using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests.Common;

/// <summary>
/// Base class for API-level integration tests (the PRIMARY confidence
/// layer per TASKFLOW-TEST-HARNESS). Boots one <see cref="TaskFlowWebApplicationFactory"/>
/// per test class (xunit class fixture semantics via <see cref="IAsyncLifetime"/>),
/// exposes an <see cref="HttpClient"/>, and truncates all tables between
/// tests so each test starts from a clean, deterministic database state.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly TaskFlowWebApplicationFactory _factory = new();

    protected HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Exposes the factory's DI container so tests can create a scoped
    /// <see cref="AppDbContext"/> for direct-DB setup (e.g. seeding a task
    /// owned by a different user than the active <c>ICurrentUserContext</c>
    /// principal) that cannot be reached through the public HTTP surface.
    /// </summary>
    protected IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Removes all rows from every mapped table. Call at the start (or end)
    /// of a test that needs a guaranteed-empty database — the factory itself
    /// is shared per test class, so state does not reset automatically.
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Tasks.ExecuteDeleteAsync();
    }
}
