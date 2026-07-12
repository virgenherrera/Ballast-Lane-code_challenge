using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using System.Threading.RateLimiting;
using TaskFlow.API.Configuration;
using TaskFlow.API.Middleware;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Tasks.Commands.CreateTask;
using TaskFlow.Application.Tasks.Commands.DeleteTask;
using TaskFlow.Application.Tasks.Commands.UpdateTask;
using TaskFlow.Application.Tasks.Queries.GetTaskById;
using TaskFlow.Application.Tasks.Queries.ListTasks;
using TaskFlow.Application.UseCases.AuthenticateUser;
using TaskFlow.Application.UseCases.RegisterUser;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;
using TaskFlow.Infrastructure.Security;

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

// Work factor 12 in production (Decision #3) — falls back to the
// BcryptOptions class default (12) when the "Bcrypt" config section is
// absent, so no env var is required to run at production strength.
builder.Services
    .AddOptions<BcryptOptions>()
    .Bind(builder.Configuration.GetSection(BcryptOptions.SectionName));

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

// Project-wide reusable TaskNotFoundException -> standard 404 error shape
// mapper. See TaskFlow.API.Middleware.TaskNotFoundExceptionHandler.
builder.Services.AddExceptionHandler<TaskNotFoundExceptionHandler>();

// Project-wide reusable DuplicateEmailException -> standard 409 error shape
// mapper. See TaskFlow.API.Middleware.DuplicateEmailExceptionHandler.
builder.Services.AddExceptionHandler<DuplicateEmailExceptionHandler>();

// Project-wide reusable InvalidCredentialsException -> standard 401 error shape
// mapper. See TaskFlow.API.Middleware.InvalidCredentialsExceptionHandler.
builder.Services.AddExceptionHandler<InvalidCredentialsExceptionHandler>();

// Fallback 401 mapper for the (rare) case where JwtCurrentUserContext throws
// due to a missing "sub" claim after the JWT middleware already authenticated
// the request. See TaskFlow.API.Middleware.UnauthorizedExceptionHandler.
builder.Services.AddExceptionHandler<UnauthorizedExceptionHandler>();
builder.Services.AddProblemDetails();

// "login" fixed-window rate-limit policy (Decision #9): 5 requests/min/IP,
// scoped ONLY to POST /api/auth/login via [EnableRateLimiting("login")] — must
// NOT be registered as the global/default limiter, or it would also throttle
// /api/auth/register and /api/tasks/*.
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            error = "TOO_MANY_REQUESTS",
            message = "Too many login attempts. Please try again later.",
            details = Array.Empty<object>(),
        }, ct);
    };

    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

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
builder.Services.AddScoped<UpdateTaskCommandHandler>();
builder.Services.AddScoped<UpdateTaskCommandValidator>();
builder.Services.AddScoped<DeleteTaskCommandHandler>();
builder.Services.AddScoped<ListTasksQueryHandler>();
builder.Services.AddScoped<ListTasksQueryValidator>();
builder.Services.AddScoped<GetTaskByIdQueryHandler>();

// User registration/auth infrastructure adapters (EP02).
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<AuthenticateUserHandler>();

// JWT-claim-backed ICurrentUserContext (EP02-B5-01). Scoped — NOT singleton —
// because it depends on IHttpContextAccessor, which is only valid for the
// lifetime of a single request. SeedCurrentUserContext.cs is preserved for
// test compatibility but is no longer wired into the production container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, JwtCurrentUserContext>();

// PostgreSQL connectivity probe backed by AspNetCore.HealthChecks.NpgSql —
// NOT an EF Core DbContext (out of scope for this batch). Failures are
// reported as "Unhealthy" by the health check framework and never throw
// past this delegate, so /health always returns 200 OK.
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql");

// JWT bearer authentication (EP02-B5-01). Validates tokens issued by
// JwtTokenService using the same Secret/Issuer/Audience (JwtOptions, bound
// above). MapInboundClaims = false is CRITICAL: without it, ASP.NET Core
// silently remaps the "sub" claim to the long ClaimTypes.NameIdentifier URI,
// and JwtCurrentUserContext's literal "sub" lookup returns null. ClockSkew
// is zeroed out — no 5-minute default leeway for this demo project.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
        ClockSkew = TimeSpan.Zero,
    };

    // Standard error shape on all JWT rejections (missing/invalid/expired
    // token) instead of ASP.NET Core's default bare 401 + WWW-Authenticate
    // header response.
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse(); // Suppress default WWW-Authenticate response
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                status = 401,
                error = "UNAUTHORIZED",
                message = "Missing, invalid, or expired authentication token.",
                details = Array.Empty<object>(),
            });
        },
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

// --- Auto-migration + demo data seeding -----------------------------------
// Runs once at startup, before the app begins serving requests. Required for
// the Docker Compose flow, where the container boots against a fresh
// PostgreSQL volume with no schema applied yet. DbSeeder is idempotent — it
// no-ops on every restart after the first successful seed.
using (var migrationScope = app.Services.CreateScope())
{
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

await DbSeeder.SeedAsync(app.Services);

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

// Enables [EnableRateLimiting("login")] on AuthController.Login. Must be
// registered before endpoint routing/execution so the "login" policy applies
// to matched endpoints.
app.UseRateLimiter();

// JWT authentication/authorization (EP02-B5-01). Must run after routing is
// resolved but before endpoints execute, so [Authorize] on TasksController
// is enforced. UseAuthentication() populates HttpContext.User from the
// bearer token; UseAuthorization() then evaluates [Authorize] attributes.
app.UseAuthentication();
app.UseAuthorization();

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
