# Handoff: EP02-B3-01 — AuthController Register Endpoint + Exception Handlers

## 1. Metadata

| Field        | Value                                                              |
| ------------ | ------------------------------------------------------------------ |
| Task ID      | EP02-B3-01                                                         |
| Task Name    | API: AuthController Register Action + DuplicateEmailExceptionHandler + DI |
| Batch        | 3 of 6 (EP02 Batch Plan)                                           |
| Epic         | EP02 — User Management                                             |
| User Stories | US-001 (AC-001.1 through AC-001.7)                                 |
| Persona      | Uncle Bob — API Layer                                              |
| Model Tier   | sonnet                                                              |

## 2. Objective

Create `AuthController` with a public `POST /api/auth/register` action that validates the
request via `RegisterUserValidator`, delegates to `RegisterUserHandler`, and returns
`201 Created` with `{ id, email, name, createdAt }`. Create `DuplicateEmailExceptionHandler`
(`IExceptionHandler`) mapping `DuplicateEmailException` to `409 Conflict` in the standard
error shape. Wire both the handler and the exception mapper into `Program.cs` DI. No
`[Authorize]` attribute — this is a public endpoint.

## 3. Pre-Conditions

- [ ] EP02-B1-01 through EP02-B1-04 STATUS: DONE — `User` entity, `Email`/`PasswordHash`
      VOs, `IUserRepository`, `IPasswordHasher`, domain exceptions, `RegisterUserCommand`,
      `RegisterUserResult`, `RegisterUserHandler`, `RegisterUserValidator` all exist and
      compile
- [ ] EP02-B2-01 through EP02-B2-04 STATUS: DONE — `AppDbContext` has `DbSet<User>`,
      `UserRepository`, `BcryptPasswordHasher`, `JwtTokenService` exist and are wired
- [ ] `dotnet build` exits 0
- [ ] No file named `AuthController.cs` exists under `src/TaskFlow.API/Controllers/`
- [ ] No file named `DuplicateEmailExceptionHandler.cs` exists under `src/TaskFlow.API/Middleware/`

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                                     | Lines   | Why                                                    |
| ------------------------------------------------------------------------ | ------- | ------------------------------------------------------ |
| `docs/user-stories/US-001-user-registration.md`                          | 48-84   | AC-001.1 through AC-001.7 — source of truth            |
| `docs/architecture/api-contract.md`                                      | 106-133 | Section 3.1 — exact request/response/error shapes      |
| `docs/epics/EP02-engineering-addenda.md`                                 | 1-19    | Decision #1 — password policy enforcement layer        |
| `docs/epics/EP02-engineering-addenda.md`                                 | 49-58   | Decision #4 — email casing rejection, not normalization |
| `src/TaskFlow.API/Controllers/TasksController.cs`                       | all     | Controller pattern: constructor injection, direct handler `.Handle(command, ct)` calls (no MediatR), validator-then-throw-ValidationException pattern |
| `src/TaskFlow.API/Middleware/ValidationExceptionHandler.cs`              | all     | `IExceptionHandler` pattern for 400 shape — register 409 the same way |
| `src/TaskFlow.API/Middleware/TaskNotFoundExceptionHandler.cs`            | all     | Simplest `IExceptionHandler` pattern — closest structural match for `DuplicateEmailExceptionHandler` |
| `src/TaskFlow.API/Program.cs`                                            | all     | DI registration pattern: `AddExceptionHandler<T>()`, `AddScoped<THandler>()`, `AddValidatorsFromAssemblyContaining<T>()` |
| `src/TaskFlow.API/Contracts/CreateTaskRequest.cs`                       | all     | Request DTO pattern: sealed record, no server-assigned fields |
| `src/TaskFlow.API/Contracts/TaskResponse.cs`                            | all     | Response DTO pattern: sealed record |

Also read the `RegisterUserCommand.cs`, `RegisterUserResult.cs`, `RegisterUserHandler.cs`,
`RegisterUserValidator.cs`, and `DuplicateEmailException.cs` files under
`src/TaskFlow.Application/UseCases/RegisterUser/` and `src/TaskFlow.Domain/Exceptions/`
(produced by EP02-B1-03) to confirm exact constructor/method signatures before wiring the
controller.

## 5. Deliverables

### Files to Create

| File Path                                                     | Contents                                                             |
| -------------------------------------------------------------- | --------------------------------------------------------------------- |
| `src/TaskFlow.API/Controllers/AuthController.cs`               | `[ApiController] [Route("api/auth")]` with `Register` action          |
| `src/TaskFlow.API/Contracts/RegisterRequest.cs`                | Sealed record: `Email`, `Name`, `Password` (all `string`)              |
| `src/TaskFlow.API/Contracts/RegisterResponse.cs`               | Sealed record: `Id` (Guid), `Email`, `Name` (string), `CreatedAt` (DateTime) |
| `src/TaskFlow.API/Middleware/DuplicateEmailExceptionHandler.cs` | `IExceptionHandler` mapping `DuplicateEmailException` to 409          |

### Files to Modify

| File Path                     | Change                                                                  |
| ------------------------------ | ------------------------------------------------------------------------ |
| `src/TaskFlow.API/Program.cs` | Register `AddExceptionHandler<DuplicateEmailExceptionHandler>()`, `AddScoped<RegisterUserHandler>()`, and confirm `AddValidatorsFromAssemblyContaining<CreateTaskCommandValidator>()` already picks up `RegisterUserValidator` (same assembly — no separate registration needed) |

### Expected Signatures

```csharp
// AuthController.cs
namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IValidator<RegisterUserCommand> _registerValidator;
    private readonly RegisterUserHandler _registerHandler;

    public AuthController(
        IValidator<RegisterUserCommand> registerValidator,
        RegisterUserHandler registerHandler)
    {
        _registerValidator = registerValidator;
        _registerHandler = registerHandler;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterUserCommand(request.Email, request.Name, request.Password);

        var validationResult = await _registerValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            // Caught by ValidationExceptionHandler (IExceptionHandler),
            // maps to the standard 400 error response shape.
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _registerHandler.Handle(command, ct);

        var response = new RegisterResponse(result.Id, result.Email, result.Name, result.CreatedAt);

        return StatusCode(StatusCodes.Status201Created, response);
        // No CreatedAtAction/Location header — no GET /api/auth/users/{id} endpoint exists.
    }
}
```

```csharp
// DuplicateEmailExceptionHandler.cs
namespace TaskFlow.API.Middleware;

public sealed class DuplicateEmailExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not DuplicateEmailException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 409,
            error = "CONFLICT",
            message = "An account with this email already exists.",
            details = Array.Empty<object>(),
        }, ct);
        return true;
    }
}
```

```csharp
// Program.cs additions (after existing AddExceptionHandler registrations):
builder.Services.AddExceptionHandler<DuplicateEmailExceptionHandler>();
// ... after AddScoped<GetTaskByIdQueryHandler>():
builder.Services.AddScoped<RegisterUserHandler>();
```

## 6. Quality Gates

| #  | Gate                          | Command                                                                       | Pass Criteria         |
| -- | ------------------------------ | ------------------------------------------------------------------------------ | ---------------------- |
| G1 | Compilation                   | `dotnet build`                                                                | exit 0                 |
| G2 | No `[Authorize]` on Register   | `grep -n "Authorize" src/TaskFlow.API/Controllers/AuthController.cs`          | zero matches           |
| G3 | Exception handler order        | Code review: `AddExceptionHandler<DuplicateEmailExceptionHandler>()` registered before or after `ValidationExceptionHandler` — order does not matter since they filter on distinct exception types | verified |
| G4 | RegisterResponse field set     | Code review: `RegisterResponse` has exactly `Id`, `Email`, `Name`, `CreatedAt` — no `Password` or `PasswordHash` field | verified |
| G5 | Full regression                | `dotnet test`                                                                 | exit 0, all existing tests pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Add `[Authorize]` to the `Register` action — this is a public endpoint (US-001 Notes)
- Add a `Login` action to `AuthController` in this task — that is EP02-B4-01
- Add a `Location` header or `CreatedAtAction` — no GET-by-id endpoint exists for users
- Implement rate limiting on `/api/auth/register` — not in scope per US-001 Out of Scope
- Modify `RegisterUserHandler`, `RegisterUserValidator`, `RegisterUserCommand`, or
  `RegisterUserResult` — those are frozen contracts from EP02-B1-03
- Modify `TasksController`, `ValidationExceptionHandler`, or `TaskNotFoundExceptionHandler`
- Send a welcome email or any notification — out of scope per US-001

### SCOPE BOUNDARY — Stop when

- `AuthController.Register` action exists, wired to `RegisterUserHandler` via DI
- `DuplicateEmailExceptionHandler` exists and is registered in `Program.cs`
- All quality gates in Section 6 pass
- Do NOT write integration tests in this task — that is EP02-B3-02
- Do NOT proceed to Batch 4 (Login) work

## 8. Anti-Patterns

| Anti-Pattern                                          | Why It Fails                                                | Do Instead                                                    |
| -------------------------------------------------------- | -------------------------------------------------------------- | ---------------------------------------------------------------- |
| Using MediatR `_mediator.Send(command, ct)`             | This project does NOT use MediatR — handlers are injected directly (see `TasksController`) | Inject the concrete handler class and call `.Handle(command, ct)` |
| Catching `DuplicateEmailException` inline in the action | Duplicates middleware logic, risks divergent error shapes    | Let the exception propagate — `DuplicateEmailExceptionHandler` handles it |
| Returning `Ok(response)` with 200 for successful register | AC-001.1 and API contract §3.1 require 201 Created            | `return StatusCode(StatusCodes.Status201Created, response);` |
| Exposing `PasswordHash` or raw password in `RegisterResponse` | Violates DOD: "No plaintext password in any log ... or test assertion output" | `RegisterResponse` carries only `{Id, Email, Name, CreatedAt}` |
| Adding `[HttpPost]` at controller root instead of `[HttpPost("register")]` | Route must be `api/auth/register`, not `api/auth`      | Use `[Route("api/auth")]` on the controller and `[HttpPost("register")]` on the action |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G5)
2. If G1 (compilation) fails: verify `RegisterUserCommand`/`RegisterUserResult`/
   `RegisterUserHandler` constructor and method signatures match exactly what EP02-B1-03
   produced — do not guess parameter names
3. If G2 fails: remove any `[Authorize]` attribute accidentally copied from a protected
   controller pattern
4. If G4 fails: confirm `RegisterResponse` maps only `result.Id, result.Email, result.Name,
   result.CreatedAt` — never the raw `RegisterUserCommand` or `User` entity
5. If G5 (regression) fails on unrelated Tasks tests: check `Program.cs` DI ordering did not
   remove or duplicate an existing registration
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and
   report FAILED with: (a) which gate, (b) full error output, (c) what was tried in each
   attempt.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS

- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Unit tests cover Domain invariants and Application use cases in isolation (mocked repos).
  Integration tests at API level remain the PRIMARY confidence layer.
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT

- Respect the current phase — do not jump ahead to Login (Batch 4) or rate limiting
- Every decision must trace back to a requirement or acceptance criterion
- Version pinning: ALL dependencies use exact versions

### TASKFLOW-BUILD-PIPELINE

- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B3-01
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm no [Authorize]; confirm 201 status code; confirm DuplicateEmailExceptionHandler registered}
```
