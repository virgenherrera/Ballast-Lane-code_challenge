# Handoff: EP02-B5-01 — JWT Auth Middleware + JwtCurrentUserContext

## 1. Metadata

| Field         | Value                                                   |
| ------------- | ------------------------------------------------------- |
| Task ID       | EP02-B5-01                                              |
| Task Name     | JWT Auth Middleware + JwtCurrentUserContext              |
| Batch         | 5 of 6 (EP02 — Protected Access)                       |
| Epic          | EP02 — User Management                                  |
| User Stories  | US-003 (AC-003.2, AC-003.3)                             |
| Persona       | Security-Aware Backend Engineer                         |
| Model Tier    | sonnet                                                  |

## 2. Objective

Register JWT bearer authentication in `Program.cs`, create `JwtCurrentUserContext` (extracts `OwnerId` from the JWT `sub` claim), replace the `SeedCurrentUserContext` DI registration, add `[Authorize]` to `TasksController`, and implement a custom 401 response handler that returns the project's standard error shape. After this task, every `/api/tasks/*` request requires a valid JWT and every unauthenticated request receives a structured 401 JSON response.

## 3. Pre-Conditions

- [ ] `dotnet build` exits 0 (full solution compiles — Batches 0-4 complete)
- [ ] `src/TaskFlow.API/Controllers/AuthController.cs` exists with `Register` and `Login` actions
- [ ] `src/TaskFlow.Infrastructure/Identity/JwtTokenService.cs` exists (token issuance working)
- [ ] `dotnet test` exits 0 (all existing unit + integration tests pass before modification)
- [ ] `src/TaskFlow.API/Configuration/JwtOptions.cs` exposes `Secret`, `Issuer`, `Audience` properties

If any pre-condition fails, report BLOCKED immediately.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                           | Lines   | Why                                          |
| -------------------------------------------------------------- | ------- | -------------------------------------------- |
| `src/TaskFlow.API/Program.cs`                                  | all     | Current middleware pipeline + DI registrations; insert auth here |
| `src/TaskFlow.API/Configuration/JwtOptions.cs`                 | all     | Options class with Secret/Issuer/Audience     |
| `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs` | all | Interface to implement (Guid OwnerId)         |
| `src/TaskFlow.Infrastructure/Identity/SeedCurrentUserContext.cs` | all   | Registration to REPLACE                       |
| `src/TaskFlow.API/Controllers/TasksController.cs`              | 1-20    | Class-level attributes — add [Authorize]      |
| `src/TaskFlow.API/Controllers/AuthController.cs`               | 1-20    | Verify NO [Authorize] at class level          |
| `docs/user-stories/US-003-protected-access.md`                 | 32-39   | DOD — no ASP.NET types in Domain/Application  |
| `src/TaskFlow.API/Middleware/`                                  | all     | Existing exception handler pattern to follow  |

## 5. Deliverables

### Files to Create

| File Path                                                    | Contents                                          |
| ------------------------------------------------------------ | ------------------------------------------------- |
| `src/TaskFlow.Infrastructure/Identity/JwtCurrentUserContext.cs` | Implements `ICurrentUserContext`, extracts `sub` from `HttpContext.User` |
| `src/TaskFlow.API/Middleware/UnauthorizedExceptionHandler.cs` | Custom 401 handler returning standard error shape |

### Files to Modify

| File Path                                          | Change                                                            |
| -------------------------------------------------- | ----------------------------------------------------------------- |
| `src/TaskFlow.API/Program.cs`                      | Add `AddAuthentication().AddJwtBearer()` + `AddAuthorization()` DI; replace `SeedCurrentUserContext` with scoped `JwtCurrentUserContext`; add `UseAuthentication()` + `UseAuthorization()` in pipeline; register `UnauthorizedExceptionHandler` |
| `src/TaskFlow.API/Controllers/TasksController.cs`  | Add `[Authorize]` attribute at class level                        |
| `src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj` | Add `Microsoft.AspNetCore.Authentication.JwtBearer` package reference if not already present |

### Expected Signatures

```csharp
// JwtCurrentUserContext.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Identity;

public sealed class JwtCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid OwnerId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
                ?? throw new InvalidOperationException("Missing 'sub' claim. Ensure [Authorize] is applied.");
            return Guid.Parse(sub);
        }
    }
}
```

```csharp
// Program.cs DI additions (near existing JwtOptions binding, replacing SeedCurrentUserContext):
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, JwtCurrentUserContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
    options.MapInboundClaims = false; // CRITICAL: prevents "sub" → ClaimTypes URI remapping
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
        ClockSkew = TimeSpan.Zero, // No leeway — strict expiry
    };
});
builder.Services.AddAuthorization();
```

```csharp
// Program.cs middleware pipeline (between UseExceptionHandler and MapControllers):
app.UseAuthentication();
app.UseAuthorization();
```

```csharp
// UnauthorizedExceptionHandler.cs
namespace TaskFlow.API.Middleware;

public sealed class UnauthorizedExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        // This handler catches the case where JwtCurrentUserContext throws
        // due to missing claims (fallback; normally JWT middleware rejects first).
        if (exception is not InvalidOperationException ioe
            || !ioe.Message.Contains("sub"))
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 401,
            error = "UNAUTHORIZED",
            message = "Missing, invalid, or expired authentication token.",
            details = Array.Empty<object>(),
        }, ct);
        return true;
    }
}
```

**Additionally**, configure a custom `JwtBearerEvents.OnChallenge` handler to return the standard error JSON body on all JWT rejections:

```csharp
// Inside AddJwtBearer options:
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
```

## 6. Quality Gates

| #  | Gate                    | Command                                                       | Pass Criteria                |
| -- | ----------------------- | ------------------------------------------------------------- | ---------------------------- |
| G1 | Compilation             | `dotnet build`                                                | exit 0                       |
| G2 | Unit tests pass         | `dotnet test tests/TaskFlow.Domain.Tests/`                    | exit 0, 0 failures           |
| G3 | Unauthenticated 401     | `dotnet test --filter "FullyQualifiedName~Tasks_WithoutToken"` (from B5-03, or manual curl) | Deferred to EP02-B5-03 |
| G4 | No ASP.NET in Domain    | `rg "Microsoft.AspNetCore" src/TaskFlow.Domain/ src/TaskFlow.Application/` | zero matches |

Note: Full integration test validation depends on EP02-B5-03 (token generation helpers). G2 validates Domain tests remain unbroken. G3 is advisory — full test validation is EP02-B5-03's responsibility.

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Add `[Authorize]` to `AuthController` (its `Register`/`Login` actions must stay public; only the future `GetMe` action gets `[Authorize]` — that's EP02-B5-02)
- Modify `SeedIdentity.cs` — preserve `SeedOwnerId` and `SeedOwnerId2` constants exactly as-is
- Add refresh token logic (out of scope per Decision #2)
- Implement rate limiting (already done in Batch 4)
- Add any authorization policies (RBAC, claims-based policies) — only authentication is needed
- Modify existing integration tests to pass (that is EP02-B5-03's job)
- Touch `src/TaskFlow.Domain/` or `src/TaskFlow.Application/` — all new code belongs in Infrastructure or API layers

### SCOPE BOUNDARY — Stop when:

- `JwtCurrentUserContext.cs` exists and implements `ICurrentUserContext`
- `Program.cs` registers JWT bearer auth, scoped `JwtCurrentUserContext`, and pipeline middleware
- `TasksController` has `[Authorize]` attribute
- Custom 401 JSON response is returned for all JWT failures
- `dotnet build` exits 0
- Do NOT proceed to `GET /api/auth/me` (EP02-B5-02) or test retrofitting (EP02-B5-03)

## 8. Anti-Patterns

| Anti-Pattern                                          | Why It Fails                                            | Do Instead                                      |
| ----------------------------------------------------- | ------------------------------------------------------- | ----------------------------------------------- |
| Not setting `MapInboundClaims = false`                | .NET remaps `sub` to a long ClaimTypes URI silently; `FindFirst("sub")` returns null | Set `options.MapInboundClaims = false` explicitly |
| Using default `ClockSkew` (5 minutes)                 | Tokens remain valid 5 min after expiry — unacceptable for demo project | Set `ClockSkew = TimeSpan.Zero`                 |
| Registering `JwtCurrentUserContext` as Singleton      | `IHttpContextAccessor` is scoped to the request; singleton causes cross-request data leaks | Register as `AddScoped<>()`                     |
| Putting `JwtCurrentUserContext` in Application layer  | Application layer must not reference `HttpContext`/`ClaimsPrincipal` | Place in `TaskFlow.Infrastructure` (or API)     |
| Hardcoding JWT secret in source code                  | Security violation; secret comes from env var via `JwtOptions` | Read from `JwtOptions` bound to configuration   |
| Using `ClaimTypes.NameIdentifier` instead of `"sub"`  | Only works if MapInboundClaims is true (default); fragile coupling to .NET's claim mapping | Use literal `"sub"` with `MapInboundClaims = false` |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G4)
2. If G1 (compilation) fails: likely missing `using` directives for `Microsoft.IdentityModel.Tokens`, `System.Text`, `Microsoft.AspNetCore.Authentication.JwtBearer`; verify the NuGet package is referenced in the Infrastructure `.csproj`
3. If G4 fails: you accidentally added ASP.NET references to Domain/Application — move the offending code to Infrastructure or API
4. If existing integration tests fail with 401 after adding `[Authorize]`: this is EXPECTED — EP02-B5-03 will fix them. Do not remove `[Authorize]` to make them pass. Confirm build compiles and domain tests pass.
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in each attempt.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Unit tests: Domain invariants + Application use cases (mocked repos)
- Integration tests: API level (AAA pattern), PRIMARY confidence layer
- E2E: Playwright, final quality gate
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT
- Respect the current phase — do not jump ahead
- Every decision must trace back to a requirement or AC
- Version pinning: ALL dependencies use exact versions

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17.5, taskflow-api, taskflow-web

### TASKFLOW-SECURITY
- JWT secret minimum 32 characters — validated at startup
- No plaintext passwords in logs, responses, or test output
- Standard error shape for all HTTP error responses (status, error, message, details)

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B5-01
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {any issues or decisions made}
```
