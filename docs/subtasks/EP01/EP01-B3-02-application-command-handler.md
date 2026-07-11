# Handoff: EP01-B3-02 — Application: DeleteTaskCommand + Handler + Unit Tests

## 1. Metadata

| Field        | Value                                                           |
| ------------ | --------------------------------------------------------------- |
| Task ID      | EP01-B3-02                                                      |
| Task Name    | Application: DeleteTaskCommand, DeleteTaskCommandHandler, unit tests |
| Batch        | 3 of N (EP01 Chunk-D)                                           |
| Epic         | EP01 — Task Management                                          |
| User Stories | US-008 (AC-008.1, AC-008.2, AC-008.4)                           |
| Persona      | Uncle Bob — Application Layer / CQRS                             |
| Model Tier   | sonnet                                                          |

## 2. Objective

Create `DeleteTaskCommand(Guid Id)` as a sealed record with a single `Id` field (no `OwnerId` — ownership is resolved by the handler via `ICurrentUserContext`, matching the established Update pattern). Create `DeleteTaskCommandHandler` that calls `_taskRepository.DeleteAsync(command.Id, _currentUserContext.OwnerId, ct)` and throws `TaskNotFoundException` when the result is `false`. No validator is needed — the command has only an `Id` field resolved from the route parameter (already parsed by the controller). Write unit tests with NSubstitute mocks covering the success path and the not-found/not-owned path.

**AUTHORITATIVE DECISION**: The handler throws `TaskNotFoundException` on `false` — it does NOT return a `Result<T>` or `OneOf`. The story's Test Plan table phrasing ("returns a not-found result, not an exception") is overridden by the QA-Auto authoritative decision: exception-based control flow, consistent with `UpdateTaskCommandHandler` and the existing `TaskNotFoundExceptionHandler` middleware.

## 3. Pre-Conditions

- [ ] EP01-B3-01 STATUS: DONE — `ITaskRepository.DeleteAsync(Guid id, Guid ownerId, CancellationToken ct)` signature exists
- [ ] `dotnet build` exits 0
- [ ] No directory `src/TaskFlow.Application/Tasks/Commands/DeleteTask/` exists
- [ ] `src/TaskFlow.Application/Common/Exceptions/TaskNotFoundException.cs` exists
- [ ] `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs` exists with `OwnerId` property

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` | all | `DeleteAsync` signature (post B3-01) |
| `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs` | all | `OwnerId` property — handler injects this |
| `src/TaskFlow.Application/Common/Exceptions/TaskNotFoundException.cs` | all | Exception type to throw on false |
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommand.cs` | all | Pattern reference: record shape |
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandHandler.cs` | all | Pattern reference: ICurrentUserContext injection, ownership resolution |
| `tests/TaskFlow.Application.Tests/Tasks/Commands/UpdateTask/UpdateTaskCommandHandlerTests.cs` | all | Test style reference: NSubstitute setup, Assert.ThrowsAsync pattern |
| `src/TaskFlow.Application/Common/Specifications/TaskOwnershipSpecification.cs` | all | Read to understand — but do NOT reuse for Delete (see Anti-Patterns) |

## 5. Deliverables

### Files to Create

| File Path | Contents |
|-----------|----------|
| `src/TaskFlow.Application/Tasks/Commands/DeleteTask/DeleteTaskCommand.cs` | Sealed record with single `Guid Id` field |
| `src/TaskFlow.Application/Tasks/Commands/DeleteTask/DeleteTaskCommandHandler.cs` | Handler: calls DeleteAsync, throws TaskNotFoundException on false |
| `tests/TaskFlow.Application.Tests/Tasks/Commands/DeleteTask/DeleteTaskCommandHandlerTests.cs` | 2 unit tests with NSubstitute mocks |

### Files to Modify

None.

### Expected Signatures

```csharp
// DeleteTaskCommand.cs
namespace TaskFlow.Application.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(Guid Id);
// No OwnerId field — handler resolves ownership via ICurrentUserContext.
// No validator needed — only Id exists and it comes from route parsing.
```

```csharp
// DeleteTaskCommandHandler.cs
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskCommandHandler
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserContext currentUserContext)
    {
        _taskRepository = taskRepository;
        _currentUserContext = currentUserContext;
    }

    public async System.Threading.Tasks.Task Handle(
        DeleteTaskCommand command,
        CancellationToken ct)
    {
        var deleted = await _taskRepository.DeleteAsync(
            command.Id, _currentUserContext.OwnerId, ct);

        if (!deleted)
        {
            throw new TaskNotFoundException(command.Id);
        }
    }
}
```

### Required Test Names

```csharp
// DeleteTaskCommandHandlerTests.cs
// Test 1: Success path
[Fact]
public async Task DeleteTaskCommandHandler_TaskExistsAndOwned_CallsDeleteAsyncOnce()
// Arrange: mock ICurrentUserContext.OwnerId returns a known Guid
//          mock ITaskRepository.DeleteAsync returns true
// Act:     handler.Handle(new DeleteTaskCommand(id), ct)
// Assert:  DeleteAsync received exactly once with (id, ownerId, ct)
//          No exception thrown

// Test 2: Not-found / not-owned path
[Fact]
public async Task DeleteTaskCommandHandler_RepositoryReturnsFalse_ThrowsTaskNotFoundException()
// Arrange: mock ICurrentUserContext.OwnerId returns a known Guid
//          mock ITaskRepository.DeleteAsync returns false
// Act/Assert: Assert.ThrowsAsync<TaskNotFoundException>(
//               () => handler.Handle(new DeleteTaskCommand(id), ct))
```

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Compilation | `dotnet build` | exit 0 |
| G2 | Handler unit tests | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~DeleteTaskCommandHandlerTests"` | exit 0, 2 tests passed |
| G3 | No Result/OneOf pattern | Code review: `DeleteTaskCommandHandler` throws `TaskNotFoundException`, does NOT return `Result<T>`, `OneOf`, or any wrapper type | verified |
| G4 | No OwnerId in command | Code review: `DeleteTaskCommand` has exactly one field: `Guid Id` — no `OwnerId`, no `Guid UserId` | verified |
| G5 | ICurrentUserContext used | Code review: handler constructor injects `ICurrentUserContext` and reads `.OwnerId` — matches UpdateTaskCommandHandler pattern | verified |
| G6 | No TaskOwnershipSpecification usage | Code review: handler does NOT call `TaskOwnershipSpecification.EnsureOwnedBy` | verified |
| G7 | Regression — all tests | `dotnet test` | exit 0, all existing tests pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Create a `DeleteTaskCommandValidator` (no payload to validate — Id comes from route)
- Introduce a `Result<T>` or `OneOf` return type — throw `TaskNotFoundException`, period
- Add an `OwnerId` field to `DeleteTaskCommand` — ownership is resolved in the handler
- Use `TaskOwnershipSpecification.EnsureOwnedBy` — that requires an entity in memory; Delete uses `ExecuteDeleteAsync` with no entity materialization
- Modify any existing file (interface, repository, controller, other commands)
- Write integration tests or E2E tests (those are later handoffs)
- Add any NuGet package

### SCOPE BOUNDARY — Stop when:

- `DeleteTaskCommand` and `DeleteTaskCommandHandler` are created
- Both unit tests pass
- All quality gates pass
- Do NOT proceed to controller or integration test work

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| `DeleteTaskCommand(Guid Id, Guid OwnerId)` | Leaks authentication concern into the command; diverges from Update's established pattern | `DeleteTaskCommand(Guid Id)` only — handler gets OwnerId from `ICurrentUserContext` |
| Calling `TaskOwnershipSpecification.EnsureOwnedBy` | Requires fetching the entity into memory first (fetch-then-check); Delete intentionally avoids entity materialization | Call `_taskRepository.DeleteAsync(id, ownerId, ct)` directly — composite predicate handles ownership at SQL level |
| Returning `Result<Unit>` or `bool` from handler | Introduces a new error-handling pattern inconsistent with Update/Get which throw TaskNotFoundException | Throw `TaskNotFoundException` — consistent with existing middleware mapping |
| Writing a validator for DeleteTaskCommand | The command has only one field (Id) which is already parsed from the route parameter by the controller; a validator with zero rules wastes a DI registration | No validator — skip entirely |

## 9. Rollback Guidance

1. If G1 fails: check that `DeleteAsync` signature in `ITaskRepository` matches exactly `Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct)` (EP01-B3-01 must be DONE)
2. If G2 fails: verify NSubstitute mock setup matches the handler's actual calls — `DeleteAsync` must be mocked with `Arg.Any<Guid>()` for flexibility or exact values
3. If G3 fails: remove any `Result<T>` wrapper and throw `TaskNotFoundException` directly
4. If G7 fails: new files should not break existing tests — check for namespace collisions or accidental file modifications
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Unit tests: Application use cases with mocked repos (NSubstitute)
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT
- Every decision must trace back to a requirement or AC
- Handler throws `TaskNotFoundException` — authoritative QA-Auto decision overrides story test-plan prose
- No `OwnerId` in command — matches UpdateTaskCommand pattern exactly

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- All dependency versions pinned

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B3-02
FILES_CREATED: [list]
FILES_MODIFIED: []
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G3 exception-based not Result; confirm G4 no OwnerId in command; confirm G6 no TaskOwnershipSpecification usage}
```
