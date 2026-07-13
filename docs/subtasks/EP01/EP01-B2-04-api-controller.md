# Handoff: EP01-B2-04 — API: TasksController PATCH Action, UpdateTaskRequest, Exception Middleware, api-contract.md Fix

## 1. Metadata

| Field        | Value                                                                        |
| ------------ | ---------------------------------------------------------------------------- |
| Task ID      | EP01-B2-04                                                                   |
| Task Name    | API: TasksController PATCH /api/tasks/{id}, UpdateTaskRequest, exception middleware, doc fix |
| Batch        | 2 of N (EP01 Chunk-U)                                                        |
| Epic         | EP01 — Task Management                                                       |
| User Stories | US-007 (AC-007.1 through AC-007.8 — this endpoint touches all eight)         |
| Persona      | Uncle Bob — API Layer                                                        |
| Model Tier   | sonnet                                                                       |

## 2. Objective

**Verified fact**: `src/TaskFlow.API/` has NO `Controllers/` directory, NO `Contracts/` directory, NO exception-handling middleware. `Program.cs` registers only health check and OpenAPI — no MVC controllers, no FluentValidation pipeline, no DI for handlers/validators/repositories. This task CREATES the full API plumbing from scratch: `TasksController` with PATCH action, `UpdateTaskRequest` dedicated DTO, `ValidationExceptionHandler` and `TaskNotFoundExceptionHandler` middleware, full DI registration in `Program.cs`, and corrects the doc-drift in `api-contract.md` section 4.4.

**PINNED DESIGN DECISION**: The route parameter is `string id` (NOT `Guid id`, NOT `{id:guid}`). A failed `{id:guid}` route constraint yields 404 by default in ASP.NET Core — directly contradicting AC-007.8 which requires 400 for malformed GUIDs. Manual `Guid.TryParse` with explicit 400 response is mandatory.

## 3. Pre-Conditions

- [ ] EP01-B2-03 STATUS: DONE — `UpdateTaskCommand`, `UpdateTaskCommandHandler`, `UpdateTaskCommandValidator` exist and pass tests
- [ ] `dotnet build` exits 0
- [ ] `src/TaskFlow.API/Program.cs` exists (confirmed — currently has health/OpenAPI only)
- [ ] No `Controllers/` or `Contracts/` directory exists under `src/TaskFlow.API/`
- [ ] No `*ExceptionHandler*` or `*ExceptionFilter*` file exists under `src/TaskFlow.API/`

If any pre-condition fails (e.g. a `TasksController.cs` was created by parallel work), report BLOCKED and re-scope to MODIFY.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `src/TaskFlow.API/Program.cs` | 1-94 (full) | Current state — must ADD MVC, DI, middleware registrations |
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommand.cs` | full (post B2-03) | Command to map request into |
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandHandler.cs` | full (post B2-03) | Handler to invoke |
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandValidator.cs` | full (post B2-03) | Validator to register in DI |
| `src/TaskFlow.Application/Common/Exceptions/TaskNotFoundException.cs` | full (post B2-02) | Exception to map to 404 |
| `src/TaskFlow.Application/Tasks/Dtos/TaskDto.cs` | 1-11 | Response shape |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | full (post B2-02) | Concrete impl to register in DI |
| `src/TaskFlow.Infrastructure/Persistence/AppDbContext.cs` | 1-21 | DbContext to register |
| `src/TaskFlow.Infrastructure/Identity/SeedCurrentUserContext.cs` | 1-10 | ICurrentUserContext impl to register |
| `docs/architecture/api-contract.md` | 63-83 (section 2.3) | Standard error envelope shape |
| `docs/architecture/api-contract.md` | 347-391 (section 4.4) | STALE section to CORRECT |
| `docs/user-stories/US-007-update-task.md` | 28-66 | Full AC set |
| `docs/user-stories/US-007-update-task.md` | 84-96 | Deliverables — dedicated UpdateTaskRequest |
| `docs/user-stories/US-007-update-task.md` | 144 | Risk: dedicated DTO rationale |

## 5. Deliverables

### Files to Create

| File Path | Contents |
|-----------|----------|
| `src/TaskFlow.API/Contracts/Tasks/UpdateTaskRequest.cs` | Sealed record, all properties optional/nullable |
| `src/TaskFlow.API/Controllers/TasksController.cs` | `[ApiController]`, `[Route("api/tasks")]`, PATCH action with `string id` parameter |
| `src/TaskFlow.API/Middleware/ValidationExceptionHandler.cs` | Maps FluentValidation `ValidationException` to 400 standard error envelope |
| `src/TaskFlow.API/Middleware/TaskNotFoundExceptionHandler.cs` | Maps `TaskNotFoundException` to 404 standard error envelope |

### Files to Modify

| File Path | Change |
|-----------|--------|
| `src/TaskFlow.API/Program.cs` | Register: AddControllers, EF Core DbContext (PostgreSQL), ITaskRepository -> TaskRepository, ICurrentUserContext -> SeedCurrentUserContext, UpdateTaskCommandHandler, UpdateTaskCommandValidator, FluentValidation (if needed), exception handlers. Add `app.MapControllers()`. |
| `docs/architecture/api-contract.md` | Section 4.4: change "AC-007.1 through AC-007.6" to "AC-007.1 through AC-007.8"; add missing error table rows for AC-007.7 and AC-007.8 |

### Expected Signatures

```csharp
// UpdateTaskRequest.cs — dedicated DTO, ZERO shared base with any Create DTO
namespace TaskFlow.API.Contracts.Tasks;

public sealed record UpdateTaskRequest(
    string? Title,
    string? Description,
    string? Status,
    DateTime? DueDate);
```

```csharp
// TasksController.cs
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Tasks.Commands.UpdateTask;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly UpdateTaskCommandHandler _updateHandler;
    private readonly UpdateTaskCommandValidator _updateValidator;

    public TasksController(
        UpdateTaskCommandHandler updateHandler,
        UpdateTaskCommandValidator updateValidator)
    {
        _updateHandler = updateHandler;
        _updateValidator = updateValidator;
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(
        string id, // NOT Guid — manual parse for AC-007.8 (400 on malformed)
        [FromBody] UpdateTaskRequest request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var taskId))
        {
            return BadRequest(new
            {
                status = 400,
                error = "VALIDATION_ERROR",
                message = "Task id is not a valid GUID.",
                details = new[] { new { field = "id", issue = "must be a valid UUID/GUID" } }
            });
        }

        var command = new UpdateTaskCommand(
            taskId, request.Title, request.Description, request.Status, request.DueDate);

        var validationResult = await _updateValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            throw new FluentValidation.ValidationException(validationResult.Errors);
        }

        var result = await _updateHandler.Handle(command, ct);
        return Ok(result);
    }
}
```

```csharp
// ValidationExceptionHandler.cs — maps ValidationException -> 400
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace TaskFlow.API.Middleware;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not ValidationException validationException)
            return false;

        httpContext.Response.StatusCode = 400;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 400,
            error = "VALIDATION_ERROR",
            message = "One or more validation errors occurred.",
            details = validationException.Errors.Select(e => new
            {
                field = char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..],
                issue = e.ErrorMessage
            })
        }, ct);
        return true;
    }
}
```

```csharp
// TaskNotFoundExceptionHandler.cs — maps TaskNotFoundException -> 404
using TaskFlow.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace TaskFlow.API.Middleware;

public sealed class TaskNotFoundExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is not TaskNotFoundException)
            return false;

        httpContext.Response.StatusCode = 404;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 404,
            error = "NOT_FOUND",
            message = "The requested task was not found.",
            details = Array.Empty<object>()
        }, ct);
        return true;
    }
}
// BOTH non-existent-id and owner-mismatch produce this identical body (AC-007.5).
```

**api-contract.md section 4.4 corrections** (explicit deliverable):
1. Line 349: `"AC-007.1 through AC-007.6"` -> `"AC-007.1 through AC-007.8"`
2. Error table: ADD row `| 400 | PATCH payload contains no updatable fields (empty body) | AC-007.7 |`
3. Error table: ADD row `| 400 | id path parameter is not a syntactically valid UUID/GUID | AC-007.8 |`

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Compilation | `dotnet build` | exit 0 (full solution) |
| G2 | Dedicated DTO | Code review: `UpdateTaskRequest` has no shared base class/interface with any CreateTaskRequest | verified |
| G3 | Route uses string id | Code review: `[HttpPatch("{id}")]` with `string id` parameter, NOT `Guid id` or `{id:guid}` | verified |
| G4 | api-contract.md corrected | `api-contract.md` section 4.4 header contains "AC-007.8" and error table has rows for AC-007.7 and AC-007.8 | verified |
| G5 | Regression — existing tests | `dotnet test` (full solution) | exit 0, all existing tests still pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Use `[HttpPatch("{id:guid}")]` — yields 404 on malformed GUID, contradicting AC-007.8
- Create a `CreateTaskRequest` DTO or POST action — this task only builds the PATCH path
- Add `[Authorize]` or any JWT middleware — Delivery 3
- Skip the `api-contract.md` fix — it is an enforced quality gate (G4)
- Implement GET, POST, DELETE actions — those are other stories (US-004/005/006/008); if they get built in parallel and a `TasksController.cs` already exists, ADD the PATCH action to it
- Use InMemory or SQLite provider for EF Core — PostgreSQL only

### SCOPE BOUNDARY — Stop when:

- PATCH action compiles, DI is registered, middleware maps exceptions correctly
- `api-contract.md` section 4.4 corrected (G4)
- Full solution builds and all existing tests pass (G5)
- Do NOT write integration tests (EP01-B2-05)

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| `[HttpPatch("{id:guid}")]` | Failed Guid route constraint = 404 by default, contradicts AC-007.8 | `string id` + explicit `Guid.TryParse` + manual 400 |
| Sharing a DTO with Create | Inherits Create's semantics silently | Fully independent `UpdateTaskRequest` |
| Two exception types or different messages for not-found vs owner-mismatch | Leaks ownership info | Single `TaskNotFoundException`, single handler, byte-identical output |
| Treating api-contract.md fix as optional | Doc drift misleads FE/QA | Enforced quality gate G4 |
| Registering DI without EF Core DbContext | Handler's `GetByIdAsync` cannot work without persistence | Full DI chain: DbContext -> TaskRepository -> Handler |

## 9. Rollback Guidance

1. If G1 fails: likely missing a NuGet package reference in the API project (e.g. `Microsoft.EntityFrameworkCore.Design`) or a missing DI registration
2. If G3 fails: change route parameter type from `Guid` to `string`
3. If `TasksController.cs` already exists from parallel work: ADD the PATCH action to it, do not recreate
4. If Program.cs registration fails: verify the connection string env vars are available or use a conditional registration pattern for builds vs runtime
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Integration tests: API level (AAA pattern), PRIMARY confidence layer
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT
- Respect the current phase — do not jump ahead into integration-test authoring
- Every decision must trace back to a requirement or AC
- Doc corrections are enforced deliverables, not incidental side effects

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B2-04
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm route uses string id (G3); confirm api-contract.md fix (G4); note if TasksController already existed from parallel work}
```
