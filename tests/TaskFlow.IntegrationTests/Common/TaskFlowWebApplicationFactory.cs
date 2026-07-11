using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests.Common;

/// <summary>
/// Reusable WebApplicationFactory backed by a real PostgreSQL container
/// (Testcontainers) — no InMemory/SQLite providers are ever used (see
/// TASKFLOW-BUILD-PIPELINE). Boots the full composition root defined in
/// <c>Program.cs</c> and applies EF Core migrations before tests run.
/// </summary>
public sealed class TaskFlowWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:17.5")
        .WithDatabase("taskflow")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public TaskFlowWebApplicationFactory()
    {
        // Program.cs runs EnvVarValidator.ValidateAndRead() as its very first
        // top-level statement — BEFORE WebApplication.CreateBuilder() and
        // therefore before ConfigureWebHost/ConfigureAppConfiguration ever
        // get a chance to run. These variables must exist in the process
        // environment ahead of time or startup fails fast with
        // InvalidOperationException. Real DB_HOST/DB_PORT values are patched
        // in after the container starts (see InitializeAsync).
        Environment.SetEnvironmentVariable("DB_HOST", "localhost");
        Environment.SetEnvironmentVariable("DB_PORT", "5432");
        Environment.SetEnvironmentVariable("DB_USER", "postgres");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "postgres");
        Environment.SetEnvironmentVariable("DB_NAME", "taskflow");
        Environment.SetEnvironmentVariable("API_PORT", "5000");
        Environment.SetEnvironmentVariable("JWT_SECRET", "test-harness-secret-not-for-production-use");
        Environment.SetEnvironmentVariable("JWT_ISSUER", "taskflow-test-harness");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "taskflow-test-harness");
    }

    public string ConnectionString => _postgresContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Replace whatever AppDbContext registration Program.cs wired up
            // with one pointed explicitly at the Testcontainers instance —
            // avoids any ambiguity from configuration precedence. Safe to
            // call even before the container starts: this only registers
            // the options delegate, the connection string itself is read
            // lazily on first DbContext resolution (after InitializeAsync
            // has already started the container).
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(opt =>
                opt.UseNpgsql(_postgresContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Force host creation now (Server getter) so the DbContext resolves
        // against the now-running container, then apply migrations.
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
