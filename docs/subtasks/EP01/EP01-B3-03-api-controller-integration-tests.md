# Handoff: EP01-B3-03 — API: TasksController DELETE Action + Integration Tests

## 1. Metadata

| Field        | Value                                                           |
| ------------ | --------------------------------------------------------------- |
| Task ID      | EP01-B3-03                                                      |
| Task Name    | API: TasksController DELETE /api/tasks/{id} + DI registration + integration tests |
| Batch        | 3 of N (EP01 Chunk-D)                                           |
| Epic         | EP01 — Task Management                                          |
| User Stories | US-008 (AC-008.1, AC-008.2, AC-008.3, AC-008.4, AC-008.5)       |
| Persona      | Uncle Bob — API Layer + Kent C. Dodds — Integration Testing      |
| Model Tier   | sonnet                                                          |

## 2. Objective

Add a `Delete` action to the existing `TasksController` mapped to `[HttpDelete("{id}")]` that uses the same `string id` + `Guid.TryParse` pattern as the existing `Update` action (returns 400 on malformed GUID for consistency, though not an AC for this story). Register `DeleteTaskCommandHandler` in DI (`Program.cs`). Write integration tests against real PostgreSQL (Testcontainers) covering all five ACs: owned delete returns 204 with empty body (AC-008.1), non-existent ID returns 404 (AC-008.2), cross-owner returns 404 (AC-008.3), repeated delete returns 404 not 500 (AC-008.4), and 204 response has no Content-Type and empty body (AC-008.5).

## 3. Pre-Conditions

- [ ] EP01-B3-01 STATUS: DONE — `ITaskRepository.DeleteAsync` and `TaskRepository.DeleteAsync` exist
- [ ] EP01-B3-02 STATUS: DONE — `DeleteTaskCommand` and `DeleteTaskCommandHandler` exist and unit tests pass
- [ ] `dotnet build` exits 0
- [ ] `src/TaskFlow.API/Controllers/TasksController.cs` exists (has GetAll, Create, Update actions)
- [ ] `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs` contains both `SeedOwnerId` and `SeedOwnerId2`

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `src/TaskFlow.API/Controllers/TasksController.cs` | all | Add Delete action here — study existing Update action pattern |
| `src/TaskFlow.API/Program.cs` | 54-86 | DI registrations — add DeleteTaskCommandHandler here |
| `src/TaskFlow.Application/Tasks/Commands/DeleteTask/DeleteTaskCommand.cs` | all | Command to instantiate |
| `src/TaskFlow.Application/Tasks/Commands/DeleteTask/DeleteTaskCommandHandler.cs` | all | Handler to invoke |
| `src/TaskFlow.Application/Common/Exceptions/TaskNotFoundException.cs` | all | Exception mapped to 404 by existing middleware |
| `src/TaskFlow.API/Middleware/TaskNotFoundExceptionHandler.cs` | all | Existing 404 error shape — DELETE reuses this |
| `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs` | all | SeedOwnerId + SeedOwnerId2 for cross-owner test |
| `tests/TaskFlow.IntegrationTests/Tasks/UpdateTaskEndpointTests.cs` | 1-60 | Test style reference: helper methods, SeedTaskDirectlyAsync pattern |
| `tests/TaskFlow.IntegrationTests/Common/IntegrationTestBase.cs` | all | Base class to extend |
| `tests/TaskFlow.IntegrationTests/Common/AssertErrorResponse.cs` | all | Shared 400 assertion helper (NOT used for 404 — 404 has different shape) |
| `docs/architecture/api-contract.md` | section 4.5 | DELETE endpoint contract: 204 No Content, 404 errors |

## 5. Deliverables

### Files to Create

| File Path | Contents |
|-----------|----------|
| `tests/TaskFlow.IntegrationTests/Tasks/DeleteTaskEndpointTests.cs` | 5+ integration tests covering all 5 ACs against real PostgreSQL |

### Files to Modify

| File Path | Change |
|-----------|--------|
| `src/TaskFlow.API/Controllers/TasksController.cs` | Add `DeleteTaskCommandHandler` to constructor injection + `Delete` action |
| `src/TaskFlow.API/Program.cs` | Add `builder.Services.AddScoped<DeleteTaskCommandHandler>();` |

### Expected Signatures

```csharp
// Addition to TasksController constructor — inject DeleteTaskCommandHandler:
private readonly DeleteTaskCommandHandler _deleteHandler;

// In constructor parameters, add:
//   DeleteTaskCommandHandler deleteHandler
// In constructor body, add:
//   _deleteHandler = deleteHandler;
```

```csharp
// New Delete action in TasksController:
[HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete(string id, CancellationToken ct)
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

    var command = new DeleteTaskCommand(taskId);
    await _deleteHandler.Handle(command, ct);

    return NoContent();
}
// TaskNotFoundException thrown by handler is caught by existing
// TaskNotFoundExceptionHandler middleware -> 404 standard error shape.
```

```csharp
// In Program.cs, add after the UpdateTaskCommandHandler registration:
builder.Services.AddScoped<DeleteTaskCommandHandler>();
```

### Required Test Names (in DeleteTaskEndpointTests.cs)

```csharp
// All tests extend IntegrationTestBase

// AC-008.1
[Fact]
public async Task DeleteTask_WithOwnedTask_Returns204AndRemovesRecord()
// Arrange: create a task via POST
// Act: DELETE /api/tasks/{id}
// Assert: 204, empty body (content length 0), direct DB query confirms row absent

// AC-008.2
[Fact]
public async Task DeleteTask_WithNonExistentId_Returns404()
// Arrange: random Guid.NewGuid()
// Act: DELETE /api/tasks/{randomId}
// Assert: 404 with standard error shape {status:404, error:"NOT_FOUND", message, details:[]}

// AC-008.3
[Fact]
public async Task DeleteTask_OwnedByAnotherUser_Returns404()
// Arrange: SeedTaskDirectlyAsync(SeedIdentity.SeedOwnerId2) — creates task under different owner
// Act: DELETE /api/tasks/{otherOwnersTaskId}
// Assert: 404 (NOT 403), standard error shape, direct DB query confirms other owner's row STILL exists

// AC-008.4
[Fact]
public async Task DeleteTask_CalledTwice_SecondCallReturns404NotServerError()
// Arrange: create task via POST
// Act: first DELETE -> 204, second DELETE same id -> 404
// Assert: second response is 404 (not 500), no unhandled exception

// AC-008.5
[Fact]
public async Task DeleteTask_SuccessResponse_HasEmptyBodyAndNoContentType()
// Arrange: create task via POST
// Act: DELETE /api/tasks/{id}
// Assert: status 204, response.Content.Headers.ContentType is null,
//         Content-Length is 0 or null, body stream length is 0
```

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Compilation | `dotnet build` | exit 0 |
| G2 | Delete integration tests | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~DeleteTaskEndpointTests"` | exit 0, 5+ tests passed |
| G3 | Route uses string id | Code review: `[HttpDelete("{id}")]` with `string id` parameter, NOT `Guid id` or `{id:guid}` | verified |
| G4 | Dual-owner seed verification | Code review: `DeleteTask_OwnedByAnotherUser_Returns404` uses `SeedIdentity.SeedOwnerId2` and asserts other owner's row still exists after DELETE | verified |
| G5 | Empty body assertion | Code review: `DeleteTask_SuccessResponse_HasEmptyBodyAndNoContentType` asserts `ContentType == null` AND body length 0, not just status code | verified |
| G6 | AC-008.4 sequential calls | Code review: `DeleteTask_CalledTwice_SecondCallReturns404NotServerError` executes TWO sequential DELETE calls in the same test, second asserts 404 | verified |
| G7 | DI registered | `grep -n "DeleteTaskCommandHandler" src/TaskFlow.API/Program.cs` | At least 1 match for `AddScoped<DeleteTaskCommandHandler>` |
| G8 | Regression — all tests | `dotnet test` | exit 0, all existing tests pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Use `[HttpDelete("{id:guid}")]` — yields 404 for malformed GUID via ASP.NET routing instead of controller-level 400
- Add `[Authorize]` or JWT middleware — Delivery 3 concern
- Modify the existing `GetAll`, `Create`, or `Update` actions
- Add a soft-delete column, global query filter, or migration
- Test malformed GUID -> 400 as a DELETE-specific AC (it is cross-cutting and out of scope per story, though the pattern is included for consistency)
- Add bulk-delete endpoint
- Modify `ITaskRepository`, `TaskRepository`, or `DeleteTaskCommandHandler`

### SCOPE BOUNDARY — Stop when:

- Delete action is wired in controller with DI registration
- All 5+ integration tests pass against real PostgreSQL
- All quality gates pass
- Do NOT proceed to Angular or E2E work

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| `[HttpDelete("{id:guid}")]` | Route constraint yields 404 for malformed GUID, diverging from Update's 400 behavior | `string id` + `Guid.TryParse` — consistent with Update |
| Catching `TaskNotFoundException` inline in the action | Duplicates middleware logic, risks divergent error shapes | Let the exception propagate — `TaskNotFoundExceptionHandler` handles it |
| Returning `Ok()` or `Ok(null)` for successful delete | AC-008.5 requires 204 No Content with empty body, not 200 | `return NoContent();` |
| Asserting only HTTP status code in integration tests | Misses empty-body, header, and row-existence assertions | Assert status + Content-Type null + body length 0 + direct DB row verification |
| Merging AC-008.2 and AC-008.4 into one test | Same code path but different arrange — must stay separate to catch regressions if persistence strategy changes | Two distinct test methods: one with never-existing id, one with previously-existing-then-deleted id |

## 9. Rollback Guidance

1. If G1 fails: check constructor injection order — `DeleteTaskCommandHandler` must be added to both the constructor parameter list and field assignment
2. If G2 fails on Testcontainers startup: verify Docker is running and the credential workaround is in place (scratch `DOCKER_CONFIG` with `credsStore` omitted)
3. If G2 fails on AC-008.3 test: verify `SeedTaskDirectlyAsync` creates a task under `SeedOwnerId2` and the DB query after DELETE confirms row persistence
4. If G7 fails: add `builder.Services.AddScoped<DeleteTaskCommandHandler>();` to `Program.cs` after the `UpdateTaskCommandHandler` registration
5. If G8 fails on existing tests: the Delete action and DI registration should not break existing endpoints — check for constructor parameter ordering issues in `TasksController`
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- Integration tests at API level (AAA pattern) are the PRIMARY confidence layer
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Tests map directly to user story acceptance criteria
- PostgreSQL via Testcontainers (postgres:17-alpine) — never InMemory/SQLite

### TASKFLOW-ANTI-DRIFT
- Every decision must trace back to a requirement or AC
- Route parameter: `string id`, NOT `Guid id`, NOT `{id:guid}` — frozen decision
- 204 No Content = empty body, no Content-Type header — per API contract section 4.5
- All 404 responses use the same standard error shape via TaskNotFoundExceptionHandler

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no EF Core InMemory, no SQLite
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web
- Docker credential workaround: scratch DOCKER_CONFIG with credsStore omitted

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B3-03
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G3 string id route; confirm G4 dual-owner test; confirm G5 empty body assertion; confirm G6 sequential delete calls; confirm G7 DI registered}
```
