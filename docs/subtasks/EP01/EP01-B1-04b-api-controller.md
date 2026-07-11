# Handoff: EP01-B1-04b — API: TasksController, Contracts, ValidationExceptionHandler, Integration Tests

## 1. Metadata

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | EP01-B1-04b                               |
| Task Name     | API: TasksController, Contracts, ValidationExceptionHandler, Integration Tests |
| Batch         | 1 of N (EP01 Chunk-C)                     |
| Epic          | EP01 — Task Management                    |
| User Stories  | US-004 (AC-004.1 through AC-004.10)      |
| Persona       | Uncle Bob — API Layer                     |
| Model Tier    | sonnet                                    |

## 2. Objective

Implement `TasksController` (`POST /api/tasks`), `CreateTaskRequest`/`TaskResponse` contracts, and `ValidationExceptionHandler` (project-wide reusable middleware for all future endpoints), with the full integration test suite proving every AC-004.1 through AC-004.10 against real PostgreSQL via the EP01-B1-04a harness. Commit a Swagger/OpenAPI snapshot as a frozen contract fixture for the frontend.

## 3. Pre-Conditions

- [ ] EP01-B1-04a reports STATUS: DONE, harness smoke test passes
- [ ] `dotnet build` exits 0
- [ ] `TaskFlowWebApplicationFactory` and `IntegrationTestBase` exist in `tests/TaskFlow.IntegrationTests/Common/`
- [ ] `CreateTaskCommand`, `CreateTaskCommandValidator`, `CreateTaskCommandHandler` exist and pass their unit tests
- [ ] `TaskRepository` exists and its dedicated tests pass (EP01-B1-03a)
- [ ] `SeedCurrentUserContext` exists (EP01-B1-03b)

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File                                                       | Lines     | Why                                          |
| ---------------------------------------------------------- | --------- | -------------------------------------------- |
| `docs/user-stories/US-004-create-task.md`                  | 29-96     | AC-004.1 through AC-004.10, DOD             |
| `docs/user-stories/US-004-create-task.md`                  | 134-159   | Test Plan table — named tests                |
| `docs/architecture/api-contract.md`                        | 64-83     | Standard error shape (section 2.3)           |
| `docs/architecture/api-contract.md`                        | 220-258   | POST /api/tasks contract (section 4.1)       |
| `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommand.cs` | all | Command to map into |
| `tests/TaskFlow.IntegrationTests/Common/TaskFlowWebApplicationFactory.cs` | all | Test harness base |
| `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs`     | all       | SeedOwnerId constant for assertions         |

## 5. Deliverables

### Files to Create

| File Path                                                              | Contents |
| ---------------------------------------------------------------------- | -------- |
| `src/TaskFlow.API/Contracts/CreateTaskRequest.cs`                      | Sealed record: Title, Description?, DueDate? — NO status/id/ownerId |
| `src/TaskFlow.API/Contracts/TaskResponse.cs`                           | Record: Id, Title, Description, Status, DueDate, OwnerId, CreatedAt, UpdatedAt |
| `src/TaskFlow.API/Controllers/TasksController.cs`                      | [HttpPost] maps request to command, returns 201 + TaskResponse |
| `src/TaskFlow.API/Middleware/ValidationExceptionHandler.cs`            | Maps ValidationException to standard error shape — project-wide, reusable by US-005+ |
| `tests/TaskFlow.IntegrationTests/Tasks/CreateTaskTests.cs`             | 14 integration tests |
| `tests/TaskFlow.IntegrationTests/Common/AssertErrorResponse.cs`        | Shared assertion helper for standard error body shape |
| `docs/architecture/openapi-snapshots/create-task-201.json`             | Frozen contract fixture: sample 201 response body |

### Files to Modify

| File Path                         | Change                                    |
| --------------------------------- | ----------------------------------------- |
| `src/TaskFlow.API/Program.cs`     | Register ValidationExceptionHandler as middleware |

### Expected Signatures

```csharp
// CreateTaskRequest.cs — AC-004.5/DOD: compile-time contract enforcement
public sealed record CreateTaskRequest(string Title, string? Description, DateTime? DueDate);
// NO Status property. NO Id. NO OwnerId. NO CreatedAt. NO UpdatedAt.

// TasksController.cs
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        // Map to CreateTaskCommand, invoke handler, return CreatedAtAction with TaskResponse
    }
}

// ValidationExceptionHandler.cs (middleware or IExceptionHandler)
// Maps FluentValidation.ValidationException -> { status: 400, error: "VALIDATION_ERROR", message: "...", details: [{field, issue}] }
```

**Required Integration Test Names (14):**

1. `CreateTask_WithEmptyTitle_Returns400` — full error body assertion
2. `CreateTask_WithWhitespaceOnlyTitle_Returns400`
3. `CreateTask_WithTitleExceeding200Chars_Returns400`
4. `CreateTask_WithTitleExactly200Chars_Returns201` — boundary-valid
5. `CreateTask_WithPastDueDate_Returns400`
6. `CreateTask_WithDueDateExactlyEqualToNow_Returns400` — exclusive boundary; use a controlled instant
7. `CreateTask_WithStatusOmittedInBody_DefaultsToPending` — AC-004.5 sub-case 1
8. `CreateTask_WithStatusSuppliedInBody_IgnoresValueAndDefaultsToPending` — AC-004.5 sub-case 2
9. `CreateTask_WithValidPayload_Returns201WithOwnerIdSet` — asserts ownerId == SeedIdentity.SeedOwnerId
10. `CreateTask_WithTitleOnly_Returns201WithNullableFieldsNull` — AC-004.9
11. `CreateTask_WithMultipleInvalidFields_Returns400WithAllDetails` — assert details.Length == 2 (AC-004.6 E2E proof)
12. `CreateTask_WithClientSuppliedIdAndOwnerId_IgnoresClientValues` — AC-004.10
13. `CreateTask_WithUnknownExtraJsonProperty_Returns201NotError` — AC-004.10 syntactic tolerance
14. `CreateTask_WithValidPayload_Returns201WithExactExpectedFieldSet` — full shape assertion (AC-004.7)

All `*_Returns400` tests MUST use `AssertErrorResponse` helper to exact-match the full standard error body (status, error, message, details with field/issue strings).

## 6. Quality Gates

| #  | Gate                          | Command                                                                                                   | Pass Criteria         |
| -- | ----------------------------- | --------------------------------------------------------------------------------------------------------- | --------------------- |
| G1 | Compilation                   | `dotnet build`                                                                                            | exit 0                |
| G2 | Integration tests             | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~CreateTaskTests"`              | exit 0, 14 passed     |
| G3 | CascadeMode E2E proof         | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~WithMultipleInvalidFields"`    | exit 0, 1 passed (isolated) |
| G4 | Contract snapshot committed   | File `docs/architecture/openapi-snapshots/create-task-201.json` exists and is valid JSON                  | verified              |
| G5 | No Status property on DTO     | Reflection: `typeof(CreateTaskRequest).GetProperties().Select(p=>p.Name)` does not contain "Status"       | verified (test exists in test suite or a dedicated unit test) |
| G6 | Previous tests still pass     | `dotnet test tests/TaskFlow.Domain.Tests/ && dotnet test tests/TaskFlow.Application.Tests/`               | exit 0                |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Re-touch Program.cs DI registration or CascadeMode global setting — already done in EP01-B1-04a (only add middleware registration here)
- Implement GET, PUT, DELETE /api/tasks — those are US-005 through US-008
- Add authentication/authorization middleware or [Authorize] attributes — Delivery 3
- Change System.Text.Json's UnmappedMemberHandling from default (Skip) — confirm it stays at default
- Create a separate unit-test project for API contracts (fold the reflection test into the integration test class or create a single file in an existing test project)

### SCOPE BOUNDARY — Stop when:

- All 14 integration tests pass, contract snapshot committed, middleware registered
- All quality gates pass
- Do NOT proceed to Frontend work

## 8. Anti-Patterns

| Anti-Pattern                                           | Why It Fails                                                  | Do Instead                                      |
| ------------------------------------------------------ | ------------------------------------------------------------- | ----------------------------------------------- |
| Treating "status ignored" as one test for both sub-cases | AC-004.5 has two distinct Givens (omitted vs. supplied)       | Two separate named tests (#7, #8)               |
| Asserting only HTTP status code on 400 responses       | Misses field/issue text regressions                            | Full-shape assertion via shared helper           |
| Using live DateTime.UtcNow in the "exactly now" test   | Race condition between request and assertion                   | Use a controlled instant or accept millisecond tolerance |
| Assuming DTO shape alone proves AC-004.10              | Compile-time argument needs runtime proof too                  | Add explicit unknown-JSON-property test (#13)    |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed
2. If G2 fails on boundary test (#4, #6): check the underlying Domain/Application comparison operators — this layer mirrors their exact semantic, does not have its own divergent logic
3. If G3 (CascadeMode E2E) fails: verify Program.cs global setting from EP01-B1-04a is picked up
4. If G5 fails: `CreateTaskRequest` has a `Status` property — remove it; compile-time enforcement
5. If tests fail with 401: the api-contract.md marks `/api/tasks` as "Protected" (Bearer required) but US-004 DOR explicitly defers auth to Delivery 3. Do NOT add [Authorize]. If the existing Program.cs has auth middleware rejecting anonymous requests, this is a BLOCKER — report to orchestrator
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

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

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B1-04b
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {any issues — especially flag if auth middleware conflict was encountered per Rollback step 5}
```
