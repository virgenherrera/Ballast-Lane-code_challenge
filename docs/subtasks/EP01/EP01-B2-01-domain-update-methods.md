# Handoff: EP01-B2-01 — Domain: TaskItem Update Methods (Rename, ChangeStatus, UpdateDescription, Reschedule)

## 1. Metadata

| Field        | Value                                                                        |
| ------------ | ---------------------------------------------------------------------------- |
| Task ID      | EP01-B2-01                                                                   |
| Task Name    | Domain: TaskItem Update Methods (Rename, ChangeStatus, UpdateDescription, Reschedule) |
| Batch        | 2 of N (EP01 Chunk-U)                                                        |
| Epic         | EP01 — Task Management                                                       |
| User Stories | US-007 (AC-007.1, AC-007.2, AC-007.3, AC-007.6)                             |
| Persona      | Uncle Bob — Domain-Driven Design                                             |
| Model Tier   | sonnet                                                                       |

## 2. Objective

Add four public instance methods to the EXISTING `TaskItem` entity — `Rename(string title)`, `ChangeStatus(TaskStatus status)`, `UpdateDescription(string? description)`, `Reschedule(DateTime? dueDate)` — each enforcing invariants symmetric with `Create()` where applicable (title non-empty/trimmed/max-length, description max-length) and explicitly DIVERGING where asymmetric: `Reschedule` MUST accept past `DateTime` values without throwing, unlike `Create()`'s dueDate rule that rejects `dueDate <= DateTime.UtcNow`. Each method independently sets `UpdatedAt = DateTime.UtcNow` unconditionally — no "skip if unchanged" optimization.

This is the CRITICAL business-risk task in Chunk-U: the past-date asymmetry between Create and Update is the story's highest-rated risk item.

## 3. Pre-Conditions

- [ ] `dotnet build src/TaskFlow.Domain/` exits 0
- [ ] `src/TaskFlow.Domain/Entities/TaskItem.cs` exists with `Create()` static factory, private setters on `Title`, `Description`, `Status`, `DueDate`, `UpdatedAt` — lines 8-15 confirm `private set`
- [ ] `src/TaskFlow.Domain/Constants/FieldLengths.cs` exists with `TitleMaxLength = 200`, `DescriptionMaxLength = 2000`
- [ ] `src/TaskFlow.Domain/Exceptions/InvalidTaskTitleException.cs` exists, sealed, extends `DomainException`
- [ ] `src/TaskFlow.Domain/Enums/TaskStatus.cs` exists with values `Pending`, `InProgress`, `Completed`
- [ ] No file named `TaskItemUpdateTests.cs` exists under `tests/TaskFlow.Domain.Tests/Entities/`

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File | Lines | Why |
|------|-------|-----|
| `src/TaskFlow.Domain/Entities/TaskItem.cs` | 1-74 (full) | The entity to MODIFY — read every member before adding methods |
| `src/TaskFlow.Domain/Constants/FieldLengths.cs` | 1-7 (full) | Constants to reuse for Rename/UpdateDescription invariants |
| `src/TaskFlow.Domain/Exceptions/InvalidTaskTitleException.cs` | 1-7 (full) | Exception to throw for title violations in Rename |
| `src/TaskFlow.Domain/Exceptions/InvalidTaskDueDateException.cs` | 1-7 (full) | Reference ONLY — do NOT reuse for Reschedule (Reschedule has no exception path) |
| `src/TaskFlow.Domain/Enums/TaskStatus.cs` | 1-8 (full) | Enum values for ChangeStatus parameter type |
| `tests/TaskFlow.Domain.Tests/Entities/TaskItemTests.cs` | 1-99 (full) | Existing Create tests — mirror AAA style; note `Task_CreateWithPastDueDate_ThrowsDomainException` on line 93-98 for the asymmetry anchor |
| `docs/user-stories/US-007-update-task.md` | 28-66 | Acceptance criteria AC-007.1 through AC-007.8 |
| `docs/user-stories/US-007-update-task.md` | 100-122 | Test Plan — exact required test names |
| `docs/user-stories/US-007-update-task.md` | 136-144 | Risks — CRITICAL past-date asymmetry |

## 5. Deliverables

### Files to Modify

| File Path | Change |
|-----------|--------|
| `src/TaskFlow.Domain/Entities/TaskItem.cs` | ADD four public instance methods after `Create()`, before the closing brace. Do NOT touch `Create()`, the private constructors, or any existing property. Do NOT add public setters. |

### Files to Create

| File Path | Contents |
|-----------|----------|
| `tests/TaskFlow.Domain.Tests/Entities/TaskItemUpdateTests.cs` | Unit tests for the four new methods including paired asymmetry proof |

### Expected Signatures

```csharp
// TaskItem.cs — ADD these four methods after Create() (line 73), before closing brace

public void Rename(string title)
{
    var trimmedTitle = title?.Trim() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(trimmedTitle))
    {
        throw new InvalidTaskTitleException("title required");
    }

    if (trimmedTitle.Length > FieldLengths.TitleMaxLength)
    {
        throw new InvalidTaskTitleException(
            $"title must not exceed {FieldLengths.TitleMaxLength} characters");
    }

    Title = trimmedTitle;
    UpdatedAt = DateTime.UtcNow;
}

public void ChangeStatus(Enums.TaskStatus status)
{
    // Free-form transition — no state machine guard (AC-007.2).
    // Do NOT add a "same status = no-op skip" — always set and bump UpdatedAt.
    Status = status;
    UpdatedAt = DateTime.UtcNow;
}

public void UpdateDescription(string? description)
{
    if (description is not null && description.Length > FieldLengths.DescriptionMaxLength)
    {
        throw new InvalidTaskTitleException(
            $"description must not exceed {FieldLengths.DescriptionMaxLength} characters");
    }

    Description = description;
    UpdatedAt = DateTime.UtcNow;
}

public void Reschedule(DateTime? dueDate)
{
    // CRITICAL ASYMMETRY (US-007 Risks): past dates are EXPLICITLY ALLOWED here.
    // Create() rejects dueDate <= DateTime.UtcNow (line 61-64) — Reschedule does NOT.
    // Do NOT copy Create's date validation. Do NOT extract a shared date-validation helper.
    DueDate = dueDate;
    UpdatedAt = DateTime.UtcNow;
}
```

### Required Test Names (in `TaskItemUpdateTests.cs`)

1. `TaskItem_Rename_WithNonEmptyTitle_UpdatesTitleAndUpdatedAt` — AC-007.1
2. `TaskItem_Rename_WithEmptyTitle_ThrowsDomainException` — AC-007.6
3. `TaskItem_Rename_WithWhitespaceOnlyTitle_ThrowsDomainException` — AC-007.6 (separate from #2)
4. `TaskItem_Rename_WithTitleExceedingMaxLength_ThrowsDomainException` — symmetric with Create
5. `TaskItem_ChangeStatus_ToAnyValidValueFromAnyCurrentStatus_UpdatesStatus` — AC-007.2, parameterized Theory covering all 9 pairs of the 3x3 matrix including `Completed` -> `Pending`
6. `TaskItem_UpdateDescription_WithValue_UpdatesDescriptionAndUpdatedAt` — AC-007.3
7. `TaskItem_UpdateDescription_WithNull_ClearsDescriptionAndUpdatesUpdatedAt` — AC-007.3
8. `TaskItem_UpdateDescription_ExceedingMaxLength_ThrowsDomainException` — symmetric with Create
9. `TaskItem_Reschedule_WithFutureDate_UpdatesDueDateAndUpdatedAt` — AC-007.3
10. `TaskItem_Reschedule_WithPastDate_DoesNotThrow` — **MUST-NOT-SKIP.** CRITICAL asymmetry proof. Place adjacent to a comment cross-referencing `TaskItemTests.Task_CreateWithPastDueDate_ThrowsDomainException` so reviewers see both.
11. `TaskItem_Reschedule_WithNull_ClearsDueDateAndUpdatesUpdatedAt` — AC-007.3

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Compilation | `dotnet build src/TaskFlow.Domain/` | exit 0 |
| G2 | New unit tests | `dotnet test tests/TaskFlow.Domain.Tests/ --filter "FullyQualifiedName~TaskItemUpdateTests"` | exit 0, all 11+ passed (Theory may expand to 9+ for status matrix), 0 failed |
| G3 | Regression — existing Create tests | `dotnet test tests/TaskFlow.Domain.Tests/ --filter "FullyQualifiedName~TaskItemTests"` | exit 0, all 10 existing tests still pass |
| G4 | Asymmetry proof isolated | `dotnet test tests/TaskFlow.Domain.Tests/ --filter "FullyQualifiedName~Reschedule_WithPastDate_DoesNotThrow"` | exit 0, 1 passed |
| G5 | Zero new PackageReference | `dotnet list src/TaskFlow.Domain/TaskFlow.Domain.csproj package` | zero packages listed (Domain has none today) |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Modify `Create()`, the private constructors, or any existing test in `TaskItemTests.cs`
- Add public setters on any property — properties stay `private set`
- Extract a shared private date-validation helper used by both `Create()` and `Reschedule()` — this is the CRITICAL anti-pattern
- Add a status-transition state machine guard rejecting certain transitions — AC-007.2 requires free-form
- Add a "skip UpdatedAt if value unchanged" optimization — DOD requires unconditional `UpdatedAt` bump
- Touch `ITaskRepository.cs`, any Application-layer file, or any Infrastructure file
- Create a new `DomainException` subclass for description (reuse `InvalidTaskTitleException` for description max-length, matching `Create()`'s existing pattern on line 57-58)

### SCOPE BOUNDARY — Stop when:

- `TaskItem.cs` has exactly four new public methods and zero other changes
- All 11 named tests pass including G4
- All quality gates pass
- Do NOT proceed to Application-layer work

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| Shared private date-validation helper between `Create()` and `Reschedule()` | Silently reintroduces past-date rejection on update — CRITICAL risk | Two fully independent methods; Reschedule has NO date-comparison branch |
| Public setters on entity properties | Bypasses invariant enforcement; lets Application layer skip validation | Keep `private set`; mutation only through the four new methods |
| Testing only one status transition (e.g. `Pending` -> `InProgress`) | Misses reverse transitions; future state-machine guard breaks silently | Parameterized Theory covering all 9 status pairs |
| Treating past-date acceptance as "implicitly covered" by not throwing | A future refactor could add date validation without any test catching it | Named, isolated test in its own quality gate (G4) |
| Different exception type for description vs title max-length | Deviates from `Create()`'s existing pattern (line 57-58 uses `InvalidTaskTitleException` for description) | Reuse same exception type to match existing behavior |

## 9. Rollback Guidance

1. Identify which gate failed (G1-G5)
2. If G2/G3 fail: verify you did NOT modify `Create()` or `TaskItemTests.cs`
3. If G4 fails: `Reschedule` still contains a date-comparison throw — remove it entirely
4. If G5 fails: revert any accidental package addition to the .csproj
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and report FAILED with: (a) which gate, (b) full error output, (c) what was tried.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Unit tests: Domain invariants + Application use cases (mocked repos)
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT
- Respect the current phase — do not jump ahead into Application/API work
- Every decision must trace back to a requirement or AC
- Version pinning: ALL dependencies use exact versions

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B2-01
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G4 asymmetry proof passed independently; note any deviation from pinned signatures}
```
