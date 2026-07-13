# Handoff: EP02-B4-01 — AuthController Login Endpoint + Rate Limiting

## 1. Metadata

| Field        | Value                                                                 |
| ------------ | ----------------------------------------------------------------------- |
| Task ID      | EP02-B4-01                                                             |
| Task Name    | API: AuthController Login Action + InvalidCredentialsExceptionHandler + Rate Limiting |
| Batch        | 4 of 6 (EP02 Batch Plan)                                               |
| Epic         | EP02 — User Management                                                 |
| User Stories | US-002 (AC-002.1 through AC-002.4)                                     |
| Persona      | Uncle Bob — API Layer                                                  |
| Model Tier   | sonnet                                                                  |

## 2. Objective

Add a public `POST /api/auth/login` action to the existing `AuthController` that validates
the request via `AuthenticateUserValidator`, delegates to `AuthenticateUserHandler`, and
returns `200 OK` with `{ accessToken, tokenType: "Bearer", expiresIn: 900, user: { id,
email, name } }`. Create `InvalidCredentialsExceptionHandler` mapping
`InvalidCredentialsException` to `401 Unauthorized`. Apply ASP.NET Core's built-in rate
limiter to `/api/auth/login` at 5 requests/min/IP, returning `429 Too Many Requests` with a
`Retry-After` header when exceeded. No `[Authorize]` attribute.

## 3. Pre-Conditions

- [ ] EP02-B3-01 STATUS: DONE — `AuthController` exists with the `Register` action wired
- [ ] EP02-B1-04 STATUS: DONE — `AuthenticateUserCommand`, `AuthenticateUserResult`,
      `AuthenticateUserHandler`, `AuthenticateUserValidator` exist and compile
- [ ] EP02-B2-04 STATUS: DONE — `JwtTokenService` (`ITokenService` implementation) exists
      and is wired in DI
- [ ] `dotnet build` exits 0
- [ ] No `Login` action exists on `src/TaskFlow.API/Controllers/AuthController.cs`
- [ ] No file named `InvalidCredentialsExceptionHandler.cs` exists under `src/TaskFlow.API/Middleware/`

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                              | Lines   | Why                                                             |
| -------------------------------------------------------------------- | ------- | ---------------------------------------------------------------- |
| `docs/user-stories/US-002-user-login.md`                         | 38-59   | AC-002.1 through AC-002.4 — source of truth                     |
| `docs/architecture/api-contract.md`                              | 142-172 | Section 3.2 — exact request/response/error shapes, incl. 429    |
| `docs/epics/EP02-engineering-addenda.md`                         | 21-34   | Decision #2 — JWT config: HS256, 15min expiry, claims sub/email/name |
| `docs/epics/EP02-engineering-addenda.md`                         | 109-124 | Decision #9 — timing-attack mitigation, rate-limit spec (5/min/IP, exponential backoff, Retry-After) |
| `src/TaskFlow.API/Controllers/AuthController.cs`                | all     | Add `Login` action here — follow existing `Register` action pattern (produced by EP02-B3-01) |
| `src/TaskFlow.API/Middleware/DuplicateEmailExceptionHandler.cs`  | all     | Closest structural match for `InvalidCredentialsExceptionHandler` (produced by EP02-B3-01) |
| `src/TaskFlow.API/Program.cs`                                    | all     | DI registration pattern + where to add `AddRateLimiter`          |

Also read `AuthenticateUserCommand.cs`, `AuthenticateUserResult.cs`,
`AuthenticateUserHandler.cs`, `AuthenticateUserValidator.cs` under
`src/TaskFlow.Application/UseCases/AuthenticateUser/`, and `InvalidCredentialsException.cs`
under `src/TaskFlow.Domain/Exceptions/` (produced by EP02-B1-04) to confirm exact
constructor/property names before wiring the controller.

## 5. Deliverables

### Files to Create

| File Path                                                            | Contents                                                          |
| ------------------------------------------------------------------------ | ---------------------------------------------------------------------- |
| `src/TaskFlow.API/Contracts/LoginRequest.cs`                        | Sealed record: `Email`, `Password` (both `string`)                |
| `src/TaskFlow.API/Contracts/LoginResponse.cs`                       | Sealed record: `AccessToken` (string), `TokenType` (string), `ExpiresIn` (int), `User` (nested `LoginUserSummary`) |
| `src/TaskFlow.API/Contracts/LoginUserSummary.cs`                    | Sealed record: `Id` (Guid), `Email`, `Name` (string)               |
| `src/TaskFlow.API/Middleware/InvalidCredentialsExceptionHandler.cs` | `IExceptionHandler` mapping `InvalidCredentialsException` to 401   |

### Files to Modify

| File Path                                        | Change                                                                            |
| --------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `src/TaskFlow.API/Controllers/AuthController.cs`  | Add `AuthenticateUserHandler`/`IValidator<AuthenticateUserCommand>` to constructor injection + `Login` action |
| `src/TaskFlow.API/Program.cs`                     | Register `AddExceptionHandler<InvalidCredentialsExceptionHandler>()`, `AddScoped<AuthenticateUserHandler>()`, `AddRateLimiter(...)` policy for `/api/auth/login`, and `app.UseRateLimiter()` in the pipeline |

### Expected Signatures

```csharp
// AuthController.cs — additions to the existing class (see EP02-B3-01 for Register):
private readonly IValidator<AuthenticateUserCommand> _loginValidator;
private readonly AuthenticateUserHandler _loginHandler;

// Constructor gains two more parameters, assigned in the body as usual.

[HttpPost("login")]
[EnableRateLimiting("login")]
[ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
{
    var command = new AuthenticateUserCommand(request.Email, request.Password);

    var validationResult = await _loginValidator.ValidateAsync(command, ct);
    if (!validationResult.IsValid)
    {
        throw new ValidationException(validationResult.Errors);
    }

    var result = await _loginHandler.Handle(command, ct);

    var response = new LoginResponse(
        result.AccessToken,
        result.TokenType,
        result.ExpiresIn,
        new LoginUserSummary(result.User.Id, result.User.Email, result.User.Name));
    // Field names on AuthenticateUserResult must be confirmed against the actual
    // EP02-B1-04 deliverable before mapping — do not assume without reading the file.

    return Ok(response);
}
```

```csharp
// InvalidCredentialsExceptionHandler.cs
namespace TaskFlow.API.Middleware;

public sealed class InvalidCredentialsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not InvalidCredentialsException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 401,
            error = "UNAUTHORIZED",
            message = "Invalid email or password.",
            details = Array.Empty<object>(),
        }, ct);
        return true;
    }
}
```

```csharp
// Program.cs additions — using Microsoft.AspNetCore.RateLimiting; System.Threading.RateLimiting;
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

builder.Services.AddExceptionHandler<InvalidCredentialsExceptionHandler>();
builder.Services.AddScoped<AuthenticateUserHandler>();

// In the middleware pipeline, after app.UseExceptionHandler() and before app.MapControllers():
app.UseRateLimiter();
```

## 6. Quality Gates

| #  | Gate                            | Command                                                              | Pass Criteria                    |
| -- | --------------------------------- | ----------------------------------------------------------------------- | ----------------------------------- |
| G1 | Compilation                     | `dotnet build`                                                       | exit 0                              |
| G2 | No `[Authorize]` on Login        | `grep -n "Authorize" src/TaskFlow.API/Controllers/AuthController.cs` | zero matches on the `Login` action (Register's absence already covered by EP02-B3-01) |
| G3 | Rate limiter policy registered  | `grep -n "AddRateLimiter" src/TaskFlow.API/Program.cs`               | at least 1 match                    |
| G4 | Rate limiter middleware wired   | `grep -n "UseRateLimiter" src/TaskFlow.API/Program.cs`               | at least 1 match, positioned before `MapControllers()` |
| G5 | LoginResponse field set         | Code review: `LoginResponse` has exactly `AccessToken`, `TokenType`, `ExpiresIn`, `User` — `TokenType` literal is `"Bearer"`, `ExpiresIn` sourced from the named constant (900), not hardcoded separately | verified |
| G6 | Full regression                 | `dotnet test`                                                       | exit 0, all existing tests pass     |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Add `[Authorize]` to the `Login` action — this is a public endpoint (US-002 Notes)
- Implement refresh tokens — explicitly out of scope per Decision #2
- Add account lockout after N failed attempts — out of scope per US-002
- Modify `AuthenticateUserHandler`, `AuthenticateUserValidator`, `AuthenticateUserCommand`,
  or `AuthenticateUserResult` — those are frozen contracts from EP02-B1-04
- Modify the `Register` action, `RegisterRequest`, `RegisterResponse`, or
  `DuplicateEmailExceptionHandler` — those belong to EP02-B3-01
- Write integration tests in this task — that is EP02-B4-02
- Implement JWT bearer authentication middleware or `GET /api/auth/me` — that is Batch 5

### SCOPE BOUNDARY — Stop when

- `AuthController.Login` action exists, wired to `AuthenticateUserHandler` via DI, decorated
  with the `"login"` rate-limit policy
- `InvalidCredentialsExceptionHandler` exists and is registered in `Program.cs`
- Rate limiter is configured (5/min/IP, fixed window, 429 + `Retry-After` on rejection)
- All quality gates in Section 6 pass
- Do NOT proceed to Batch 5 (Protected Access / JWT middleware) work

## 8. Anti-Patterns

| Anti-Pattern                                                | Why It Fails                                                          | Do Instead                                                              |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------ | --------------------------------------------------------------------------- |
| Using MediatR `_mediator.Send(command, ct)`                     | This project does NOT use MediatR — handlers are injected directly    | Inject `AuthenticateUserHandler` and call `.Handle(command, ct)`          |
| Returning a distinct message for "user not found" vs. "wrong password" | AC-002.2 requires the IDENTICAL generic message for both — the exception already guarantees this if the handler is used correctly | Let `InvalidCredentialsException`'s single message flow through unchanged |
| Hardcoding `expiresIn: 900` separately in the controller         | Diverges from the named constant if the JWT config ever changes        | Map `result.ExpiresIn` straight from `AuthenticateUserResult` — the constant lives in Application/Infrastructure, not the controller |
| Using a global rate limiter (`app.UseRateLimiter()` with a default limiter for all endpoints) | Would throttle `/api/auth/register` and `/api/tasks/*` unintentionally | Scope the limiter to a named policy (`"login"`) applied only via `[EnableRateLimiting("login")]` on the `Login` action |
| Omitting the `Retry-After` header on 429                        | API contract §3.2 explicitly requires it                               | Set `context.HttpContext.Response.Headers.RetryAfter` in `OnRejected`     |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G6)
2. If G1 fails: verify `AuthenticateUserCommand`/`AuthenticateUserResult` field names match
   exactly what EP02-B1-04 produced — do not guess the shape of the nested user summary
3. If G3/G4 fail: confirm `using Microsoft.AspNetCore.RateLimiting;` and
   `using System.Threading.RateLimiting;` are present, and `UseRateLimiter()` is placed
   after `UseExceptionHandler()` but before `MapControllers()`
4. If G6 (regression) fails on unrelated Tasks tests: check `Program.cs` DI ordering did
   not remove or duplicate an existing registration when adding the rate limiter block
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and
   report FAILED with: (a) which gate, (b) full error output, (c) what was tried in each
   attempt.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS

- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT

- Respect the current phase — do not jump ahead to JWT middleware or `/api/auth/me` (Batch 5)
- Every decision must trace back to a requirement or acceptance criterion
- Version pinning: ALL dependencies use exact versions

### TASKFLOW-BUILD-PIPELINE

- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B4-01
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm no [Authorize]; confirm rate limiter policy + middleware wired; confirm 429 includes Retry-After}
```
