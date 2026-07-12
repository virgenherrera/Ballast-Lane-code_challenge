using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Infrastructure.Identity;
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

    /// <summary>Unauthenticated client — no Authorization header. Required for
    /// 401 tests (missing/expired/tampered token) and any pre-auth endpoint
    /// (e.g. /api/auth/register, /api/auth/login, /health). Do NOT remove:
    /// AuthenticatedClient below does not replace this.</summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>Client pre-configured with a valid Bearer token for
    /// <see cref="SeedIdentity.SeedOwnerId"/>. Use for all happy-path
    /// requests to [Authorize]-protected endpoints (e.g. /api/tasks) now
    /// that EP02-B5-01 enforces JWT auth on TasksController.</summary>
    protected HttpClient AuthenticatedClient { get; private set; } = null!;

    /// <summary>
    /// Exposes the factory's DI container so tests can create a scoped
    /// <see cref="AppDbContext"/> for direct-DB setup (e.g. seeding a task
    /// owned by a different user than the active <c>ICurrentUserContext</c>
    /// principal) that cannot be reached through the public HTTP surface.
    /// </summary>
    protected IServiceProvider Services => _factory.Services;

    /// <summary>
    /// Exposes the underlying factory so tests can mint custom tokens (e.g.
    /// for a non-default owner, or deliberately expired/tampered) via
    /// <see cref="TaskFlowWebApplicationFactory.GenerateTestToken"/> and its
    /// sibling helpers, rather than only ever using the fixed
    /// <see cref="AuthenticatedClient"/> principal.
    /// </summary>
    protected TaskFlowWebApplicationFactory Factory => _factory;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        Client = _factory.CreateClient();

        AuthenticatedClient = _factory.CreateClient();
        var token = _factory.GenerateTestToken(SeedIdentity.SeedOwnerId);
        AuthenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        AuthenticatedClient.Dispose();
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
