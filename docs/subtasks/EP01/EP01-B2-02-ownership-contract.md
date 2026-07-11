# Handoff: EP01-B2-02 — Application: Ownership Contract (ITaskRepository Extension, TaskNotFoundException, Ownership Specification)

## 1. Metadata

| Field        | Value                                                                        |
| ------------ | ---------------------------------------------------------------------------- |
| Task ID      | EP01-B2-02                                                                   |
| Task Name    | Application: Ownership Contract (Repository read method, Not-Found exception, shared ownership helper) |
| Batch        | 2 of N (EP01 Chunk-U)                                                        |
| Epic         | EP01 — Task Management                                                       |
| User Stories | US-007 (AC-007.5) — establishes shared infrastructure for US-005/US-006/US-008 reuse |
| Persona      | Uncle Bob — Application Layer / Ports & Adapters                             |
| Model Tier   | sonnet                                                                       |

## 2. Objective

Extend the Application layer with the ownership-check contract required by US-007 and all sibling task stories. Verified facts: `ITaskRepository` currently exposes ONLY `Task AddAsync(TaskItem task, CancellationToken ct)`. No `GetByIdAsync`, no not-found exception, and no ownership-check helper exist anywhere in the codebase. US-005/US-006/US-008 have NOT been implemented. This task ESTABLISHES the shared ownership contract for the first time — it does not "reuse" anything. The deliverables are: (1) add `GetByIdAsync` to `ITaskRepository`, (2) create `TaskNotFoundException`, (3) create `TaskOwnershipSpecification` as the single-source-of-truth helper that all current and future stories (Get/ViewDetail/Update/Delete) MUST call.

Additionally, extend the concrete `TaskRepository` in Infrastructure to implement `GetByIdAsync` — since this is the only place that can fulfil the new interface method.

## 3. Pre-Conditions

- [ ] EP01-B2-01 reports STATUS: DONE — `TaskItem` has Rename/ChangeStatus/UpdateDescription/Reschedule
- [ ] `dotnet build` exits 0 for the full solution
- [ ] `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` exists with exactly one method (`AddAsync`)
- [ ] `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` exists implementing `ITaskRepository` with only `AddAsync`
- [ ] No file matching `*NotFoundException*` exists under `src/TaskFlow.Application/`
- [ ] No file matching `*OwnershipSpecification*` exists under `src/TaskFlow.Application/`

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` | 1-8 (full) | Interface to MODIFY — currently has only `AddAsync` |
| `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs` | 1-6 (full) | `OwnerId` property used by the ownership check |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | 1-20 (full) | Concrete class to MODIFY — add `GetByIdAsync` implementation |
| `src/TaskFlow.Infrastructure/Persistence/AppDbContext.cs` | 1-21 (full) | `DbSet<TaskItem> Tasks` — used for the query |
| `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommandHandler.cs` | 1-46 (full) | Pattern reference for how handlers consume ITaskRepository — do NOT modify |
| `docs/user-stories/US-007-update-task.md` | 48-51 | AC-007.5: identical 404 for both cases |
| `docs/user-stories/US-007-update-task.md` | 77 | DOD: "Ownership check reuses the exact same helper/specification" |

## 5. Deliverables

### Files to Modify

| File Path | Change |
|-----------|--------|
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` | ADD `Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct)` — do NOT change `AddAsync` |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | ADD implementation of `GetByIdAsync` using `_dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct)` |

### Files to Create

| File Path | Contents |
|-----------|----------|
| `src/TaskFlow.Application/Common/Exceptions/TaskNotFoundException.cs` | Sealed exception for "task does not exist OR not owned by caller" — single type for both AC-007.5 sub-cases |
| `src/TaskFlow.Application/Common/Specifications/TaskOwnershipSpecification.cs` | Static helper: validates task exists AND belongs to caller, throws `TaskNotFoundException` otherwise |
| `tests/TaskFlow.Application.Tests/Common/Specifications/TaskOwnershipSpecificationTests.cs` | 3 unit tests for the specification |

### Expected Signatures

```csharp
// ITaskRepository.cs — MODIFY: add method, keep AddAsync unchanged
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken ct);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct);
}
```

```csharp
// TaskRepository.cs — MODIFY: add implementation
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _dbContext;

    public TaskRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct)
    {
        await _dbContext.Tasks.AddAsync(task, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
```

```csharp
// TaskNotFoundException.cs — CREATE
namespace TaskFlow.Application.Common.Exceptions;

public sealed class TaskNotFoundException : Exception
{
    public TaskNotFoundException(Guid taskId)
        : base($"Task '{taskId}' was not found.")
    {
    }
}
```

```csharp
// TaskOwnershipSpecification.cs — CREATE
// Single source of truth for "does this task exist AND belong to this owner".
// Reused by UpdateTaskCommandHandler (this batch) and intended for all future
// Get/ViewDetail/Delete handlers (US-005/006/008).
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Specifications;

public static class TaskOwnershipSpecification
{
    /// <summary>
    /// Returns the task if it exists AND is owned by the caller.
    /// Throws TaskNotFoundException for BOTH non-existent and owner-mismatch cases
    /// so that no ownership information is leaked (AC-007.5).
    /// </summary>
    public static TaskItem EnsureOwnedBy(TaskItem? task, Guid ownerId, Guid requestedTaskId)
    {
        if (task is null || task.OwnerId != ownerId)
        {
            throw new TaskNotFoundException(requestedTaskId);
        }

        return task;
    }
}
```

### Required Test Names (`TaskOwnershipSpecificationTests.cs`)

1. `TaskOwnershipSpecification_EnsureOwnedBy_WithNullTask_ThrowsTaskNotFoundException` — non-existent case
2. `TaskOwnershipSpecification_EnsureOwnedBy_WithMismatchedOwnerId_ThrowsTaskNotFoundException` — owner-mismatch case
3. `TaskOwnershipSpecification_EnsureOwnedBy_WithMatchingOwnerId_ReturnsTask` — happy path

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Compilation | `dotnet build` | exit 0 (full solution) |
| G2 | Specification tests | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~TaskOwnershipSpecificationTests"` | exit 0, 3 passed |
| G3 | Backward compatibility | `dotnet build src/TaskFlow.Application/` | `CreateTaskCommandHandler` still compiles unmodified |
| G4 | Regression — existing Create tests | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~CreateTaskCommand"` | exit 0, all existing tests pass |
| G5 | Regression — existing Integration tests | `dotnet test tests/TaskFlow.IntegrationTests/` | exit 0, `TaskRepositoryTests` still pass |
| G6 | Single exception type | Verify exactly one file matching `*NotFoundException*` under `src/TaskFlow.Application/` | confirmed |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Implement `UpdateTaskCommand`, `UpdateTaskCommandHandler`, or `UpdateTaskCommandValidator` — EP01-B2-03
- Modify `CreateTaskCommandValidator.cs`, `CreateTaskCommand.cs`, or `CreateTaskCommandHandler.cs`
- Add a shared base validator class for Create/Update — validators remain fully independent
- Add authentication/authorization logic — ownership check uses `ICurrentUserContext.OwnerId` (seed for Delivery 1)
- Add a `SaveChangesAsync` method to `ITaskRepository` — EF Core change tracking on the fetched entity handles persistence implicitly when the same DbContext instance calls `SaveChangesAsync` later (handler responsibility in EP01-B2-03)
- Create a `SeedIdentity` second owner ID — that will be needed in EP01-B2-05 (integration tests), not here

### SCOPE BOUNDARY — Stop when:

- `ITaskRepository` has exactly two methods (`AddAsync`, `GetByIdAsync`)
- `TaskRepository` concrete class implements both
- `TaskNotFoundException` and `TaskOwnershipSpecification` exist and are tested
- All quality gates pass
- Do NOT proceed to EP01-B2-03

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| Two different exception types for "not found" vs "wrong owner" | Leaks ownership information — violates AC-007.5's identical-body requirement | Single `TaskNotFoundException` for both cases |
| Implementing ownership check inline in a handler | Each story reimplements independently, risking divergent behavior | Single `TaskOwnershipSpecification.EnsureOwnedBy` helper |
| Making `GetByIdAsync` filter by ownerId inside the repository | Conflates existence and ownership; makes the "identical 404" proof harder | Pure existence lookup; ownership comparison in the specification |
| Throwing `TaskNotFoundException` from inside `TaskItem` domain methods | Domain must stay free of Application-level concerns | Exception lives entirely in `TaskFlow.Application` |
| Returning different `.Message` strings for null vs mismatch | A caller could distinguish cases by reading the message | Same constructor, same message format for both |

## 9. Rollback Guidance

1. If G3 fails: you likely renamed/removed `AddAsync` — the extension must be purely additive
2. If G5 fails: check the `TaskRepository` changes did not alter `AddAsync` behavior
3. If G2 fails: check null-check ordering in `EnsureOwnedBy` — both conditions must map to same exception
4. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Unit tests: Domain invariants + Application use cases (mocked repos)
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT
- Respect the current phase — do not jump ahead into API work
- Every decision must trace back to a requirement or AC
- This task ESTABLISHES shared infrastructure — US-005/006/008 do not exist yet; note this in STATUS

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17.5, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B2-02
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm this is the FIRST implementation of the ownership-check helper (not a reuse); flag that DOD wording "reuses" should read "establishes" until US-005/006/008 exist}
```
