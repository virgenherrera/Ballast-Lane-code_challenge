using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using TaskFlow.API.Configuration;
using TaskFlow.API.Middleware;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Commands.CreateTask;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;

// --- Fail-fast environment validation -------------------------------------
// Must run before builder.Build() / app.Run() so a misconfigured environment
// never reaches a listening state (see EP01-B0-03 quality gate G5).
var envVars = EnvVarValidator.ValidateAndRead();

var builder = WebApplication.CreateBuilder(args);

// --- Options pattern: bind config sections from validated env vars --------
// Handlers must never read IConfiguration directly — they consume typed
// options instead.
var connectionString = new NpgsqlConnectionStringBuilder
{
    Host = envVars["DB_HOST"],
    Port = int.Parse(envVars["DB_PORT"]),
    Username = envVars["DB_USER"],
    Password = envVars["DB_PASSWORD"],
    Database = envVars["DB_NAME"],
}.ConnectionString;

// TD: When EF Core is configured, add startup DB connectivity validation here
// (throw if DbContext.Database.CanConnectAsync() fails)

builder.Configuration["Database:ConnectionString"] = connectionString;
builder.Configuration["Jwt:Secret"] = envVars["JWT_SECRET"];
builder.Configuration["Jwt:Issuer"] = envVars["JWT_ISSUER"];
builder.Configuration["Jwt:Audience"] = envVars["JWT_AUDIENCE"];

builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName));

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));

// --- Kestrel: bind to API_PORT from environment, not a hardcoded port -----
var apiPort = envVars["API_PORT"];
builder.WebHost.UseUrls($"http://0.0.0.0:{apiPort}");

// --- Services ---------------------------------------------------------
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Project-wide reusable ValidationException -> standard error shape mapper.
// See TaskFlow.API.Middleware.ValidationExceptionHandler for the response
// contract.
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

// --- Composition root: EF Core + FluentValidation + repositories/identity -
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? connectionString));

// SET ONCE, GLOBALLY — do NOT set CascadeMode per-validator (see
// CreateTaskCommandValidator remarks). Continue mode ensures every rule
// runs and every failure is collected, instead of stopping at the first.
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue;

builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskCommandValidator>();

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<CreateTaskCommandHandler>();

// TODO(Delivery-3): Replace with a JWT-claim-backed ICurrentUserContext
// registration. Singleton is safe ONLY because the seed shim is stateless.
builder.Services.AddSingleton<ICurrentUserContext, SeedCurrentUserContext>();

// PostgreSQL connectivity probe backed by AspNetCore.HealthChecks.NpgSql —
// NOT an EF Core DbContext (out of scope for this batch). Failures are
// reported as "Unhealthy" by the health check framework and never throw
// past this delegate, so /health always returns 200 OK.
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

// Placeholder — future DI registration:
// - Auth middleware / JWT bearer authentication (uses JwtOptions above)

var app = builder.Build();

// Captured once at startup and reused — never recomputed per request.
var liveSince = DateTimeOffset.UtcNow;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Project-wide ValidationException -> standard error shape mapping. Must be
// registered before endpoint routing/execution so it can catch exceptions
// thrown by any controller action.
app.UseExceptionHandler();

// Placeholder — future middleware:
// - JWT authentication / authorization middleware

app.MapControllers();

// GET /health — public, outside the /api prefix, no authentication.
// Always returns 200 OK, even when PostgreSQL is unreachable.
app.MapGet("/health", async (HealthCheckService healthCheckService) =>
{
    var report = await healthCheckService.CheckHealthAsync();
    var dbEntry = report.Entries.TryGetValue("postgresql", out var entry)
        ? entry
        : (HealthReportEntry?)null;

    var dbStatus = dbEntry?.Status == HealthStatus.Healthy ? "ok" : "down";

    return Results.Ok(new
    {
        status = "ok",
        liveSince,
        db = dbStatus,
    });
});

app.Run();

// Exposes the implicit Program class generated from top-level statements so
// WebApplicationFactory<Program> (integration tests) can reference it.
public partial class Program
{
}
