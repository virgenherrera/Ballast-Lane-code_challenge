using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Security;

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

            // Decision #3: production BCrypt work factor (12) costs ~250ms per
            // hash — far too slow across an integration suite that registers
            // many users. Replace with work factor 4 here so tests stay fast;
            // never use factor 4 outside this test harness.
            var hasherDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IPasswordHasher));

            if (hasherDescriptor is not null)
            {
                services.Remove(hasherDescriptor);
            }

            services.AddScoped<IPasswordHasher>(_ =>
                new BcryptPasswordHasher(Options.Create(new BcryptOptions { WorkFactor = 4 })));
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

        await SeedIdentityUsersAsync(dbContext, scope.ServiceProvider);
    }

    /// <summary>
    /// Inserts the two well-known <see cref="SeedIdentity"/> rows directly
    /// into "users" via raw SQL (bypassing <c>User.Create</c>, which always
    /// mints a fresh Guid v7 id and cannot be pointed at a fixed test id).
    /// Required for GET /api/auth/me (EP02-B5-02), which 404s if the JWT
    /// "sub" claim does not resolve to a real row via IUserRepository.
    /// ON CONFLICT DO NOTHING makes this idempotent — safe to call once per
    /// factory even though Tasks (not Users) are truncated between tests
    /// within the same test class (see IntegrationTestBase.ResetDatabaseAsync).
    /// </summary>
    private static async Task SeedIdentityUsersAsync(AppDbContext dbContext, IServiceProvider services)
    {
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var passwordHash = hasher.Hash("Test-Harness-Seed-Password-1!").Value;
        var now = DateTime.UtcNow;

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO users (id, email, name, password_hash, created_at)
             VALUES ({SeedIdentity.SeedOwnerId}, {SeedIdentity.SeedOwnerEmail}, {SeedIdentity.SeedOwnerName}, {passwordHash}, {now})
             ON CONFLICT (id) DO NOTHING
             """);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO users (id, email, name, password_hash, created_at)
             VALUES ({SeedIdentity.SeedOwnerId2}, {SeedIdentity.SeedOwnerId2Email}, {SeedIdentity.SeedOwnerId2Name}, {passwordHash}, {now})
             ON CONFLICT (id) DO NOTHING
             """);
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Issues an HS256 JWT signed with the exact JWT_SECRET/JWT_ISSUER/JWT_AUDIENCE
    /// this factory injects into the process environment (see constructor) —
    /// the same values <c>Program.cs</c> binds into <c>JwtOptions</c> and the
    /// JWT bearer handler validates against. Claim set mirrors
    /// <see cref="TaskFlow.Infrastructure.Identity.JwtTokenService"/> exactly
    /// (sub/email/name) so test tokens are indistinguishable from real ones.
    /// Defaults to <see cref="SeedIdentity.SeedOwnerId"/> so tests that don't
    /// care about identity can call this with no arguments.
    /// </summary>
    public string GenerateTestToken(
        Guid userId = default,
        string? email = null,
        string? name = null,
        TimeSpan? expiry = null)
    {
        var resolvedUserId = userId == default ? SeedIdentity.SeedOwnerId : userId;
        var resolvedEmail = email ?? SeedIdentity.SeedOwnerEmail;
        var resolvedName = name ?? SeedIdentity.SeedOwnerName;

        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")!;
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")!;
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", resolvedUserId.ToString()),
            new Claim("email", resolvedEmail),
            new Claim("name", resolvedName),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(15)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Same claim set as <see cref="GenerateTestToken"/> but with an
    /// already-elapsed expiry, so JWT bearer's <c>ValidateLifetime</c> check
    /// rejects it with 401 — no <c>Thread.Sleep</c> required.
    /// </summary>
    public string GenerateExpiredTestToken(Guid userId = default)
    {
        return GenerateTestToken(userId, expiry: TimeSpan.FromMinutes(-5));
    }

    /// <summary>
    /// Structurally valid JWT signed with a key the API never configured, so
    /// <c>ValidateIssuerSigningKey</c> rejects it with 401 — proves signature
    /// tampering is caught, not just expiry/absence.
    /// </summary>
    public string GenerateTamperedTestToken(Guid userId = default)
    {
        var resolvedUserId = userId == default ? SeedIdentity.SeedOwnerId : userId;

        var wrongKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("this-is-a-wrong-key-for-testing-tampered-tokens!"));
        var credentials = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] { new Claim("sub", resolvedUserId.ToString()) };
        var token = new JwtSecurityToken(
            issuer: Environment.GetEnvironmentVariable("JWT_ISSUER")!,
            audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE")!,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
