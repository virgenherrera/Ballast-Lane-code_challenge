# Handoff: EP01-B1-01 — Domain: TaskItem Entity, TaskStatus, Exceptions, FieldLengths

## 1. Metadata

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | EP01-B1-01                                |
| Task Name     | Domain: TaskItem Entity, TaskStatus, Exceptions, FieldLengths |
| Batch         | 1 of N (EP01 Chunk-C)                     |
| Epic          | EP01 — Task Management                    |
| User Stories  | US-004 (AC-004.1, AC-004.3, AC-004.4, AC-004.7, AC-004.8) |
| Persona       | Uncle Bob — Domain-Driven Design          |
| Model Tier    | sonnet                                    |

## 2. Objective

Create the `TaskItem` Domain entity with a private constructor and `Create()` static factory that enforces all Domain invariants (title required/trimmed/max 200 chars, description max 2000 chars, dueDate strictly future, UUID v7 generated at construction), plus the `TaskStatus` enum, two Domain exception types, and a shared `FieldLengths` constants class. Zero external package references. Zero I/O. Pure C# with 10 unit tests proving every boundary.

## 3. Pre-Conditions

- [ ] `dotnet build src/TaskFlow.Domain/` exits 0 (project exists from scaffold)
- [ ] `dotnet build tests/TaskFlow.Domain.Tests/` exits 0
- [ ] `src/TaskFlow.Domain/TaskFlow.Domain.csproj` has zero `<PackageReference>` elements
- [ ] .NET 10.0 SDK installed (`dotnet --version` reports 10.x — needed for `Guid.CreateVersion7()`)
- [ ] No file named `TaskItem.cs`, `TaskStatus.cs`, or `FieldLengths.cs` exists under `src/TaskFlow.Domain/`

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                              | Lines     | Why                                          |
| ------------------------------------------------- | --------- | -------------------------------------------- |
| `docs/user-stories/US-004-create-task.md`         | 13-27     | DOR — entity contract, field constraints     |
| `docs/user-stories/US-004-create-task.md`         | 29-79     | AC-004.1 through AC-004.10 — source of truth for all invariants |
| `docs/user-stories/US-004-create-task.md`         | 161-171   | Validation Rules — exact boundary semantics  |
| `docs/architecture/clean-architecture.md`         | 103-164   | Project structure and folder layout for Domain project |

## 5. Deliverables

### Files to Create

| File Path                                                        | Contents |
| ---------------------------------------------------------------- | -------- |
| `src/TaskFlow.Domain/Constants/FieldLengths.cs`                  | Static class: `TitleMaxLength = 200`, `DescriptionMaxLength = 2000` |
| `src/TaskFlow.Domain/Enums/TaskStatus.cs`                        | Enum: `Pending`, `InProgress`, `Completed` |
| `src/TaskFlow.Domain/Exceptions/DomainException.cs`              | Abstract base exception |
| `src/TaskFlow.Domain/Exceptions/InvalidTaskTitleException.cs`    | Sealed, extends DomainException |
| `src/TaskFlow.Domain/Exceptions/InvalidTaskDueDateException.cs`  | Sealed, extends DomainException |
| `src/TaskFlow.Domain/Entities/TaskItem.cs`                       | Entity with private ctor + static factory |
| `tests/TaskFlow.Domain.Tests/Entities/TaskItemTests.cs`          | 10 unit tests |

### Files to Modify

| File Path                              | Change                                    |
| -------------------------------------- | ----------------------------------------- |
| `src/TaskFlow.Domain/Class1.cs`        | DELETE this placeholder file               |

### Expected Signatures

```csharp
// FieldLengths.cs
namespace TaskFlow.Domain.Constants;
public static class FieldLengths
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
}

// TaskItem.cs
namespace TaskFlow.Domain.Entities;
public sealed class TaskItem
{
    public Guid Id { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public Enums.TaskStatus Status { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid OwnerId { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private TaskItem() { } // EF Core parameterless ctor (private)

    public static TaskItem Create(string title, string? description, DateTime? dueDate, Guid ownerId)
    {
        // 1. var trimmed = title?.Trim(); if string.IsNullOrWhiteSpace(trimmed) -> throw InvalidTaskTitleException
        // 2. if trimmed.Length > FieldLengths.TitleMaxLength -> throw InvalidTaskTitleException
        // 3. if description?.Length > FieldLengths.DescriptionMaxLength -> throw InvalidTaskTitleException (reuse, or create description-specific — pick ONE, see Boundaries)
        // 4. if dueDate.HasValue && dueDate.Value <= DateTime.UtcNow -> throw InvalidTaskDueDateException
        //    NOTE: MUST be <= (exclusive — equal-to-now is rejected per AC-004.4)
        // 5. var id = Guid.CreateVersion7(); var now = DateTime.UtcNow;
        // 6. Status = TaskStatus.Pending; CreatedAt = now; UpdatedAt = now;
    }
}
```

**Required Test Names** (10 tests, zero mocks, zero I/O):

1. `Task_CreateWithValidData_SetsStatusToPendingAndAssignsId`
2. `Task_CreateWithEmptyTitle_ThrowsDomainException`
3. `Task_CreateWithWhitespaceOnlyTitle_ThrowsDomainException`
4. `Task_CreateWithNbspOnlyTitle_ThrowsDomainException` — title = `" "`
5. `Task_CreateWithTitleExactly200Chars_Succeeds` — boundary-valid
6. `Task_CreateWithTitleExceedingMaxLength_ThrowsDomainException` — 201 chars
7. `Task_CreateWithDescriptionExactly2000Chars_Succeeds` — boundary-valid
8. `Task_CreateWithDescriptionExceeding2000Chars_ThrowsDomainException`
9. `Task_CreateWithDueDateExactlyEqualToNow_ThrowsDomainException` — exclusive boundary
10. `Task_CreateWithPastDueDate_ThrowsDomainException`

## 6. Quality Gates

| #  | Gate                    | Command                                                                          | Pass Criteria              |
| -- | ----------------------- | -------------------------------------------------------------------------------- | -------------------------- |
| G1 | Compilation             | `dotnet build src/TaskFlow.Domain/`                                              | exit 0                     |
| G2 | Unit tests              | `dotnet test tests/TaskFlow.Domain.Tests/ --filter "FullyQualifiedName~TaskItemTests"` | exit 0, 10 passed, 0 failed |
| G3 | Zero PackageReference   | `dotnet list src/TaskFlow.Domain/TaskFlow.Domain.csproj package`                 | output shows no packages   |
| G4 | No speculative types    | Count of `class.*Exception` in `src/TaskFlow.Domain/Exceptions/` = exactly 3     | DomainException, InvalidTaskTitleException, InvalidTaskDueDateException |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Add any NuGet package to `TaskFlow.Domain.csproj` — zero PackageReferences is a hard DOD item
- Create Application-layer interfaces (`ITaskRepository`, `ICurrentUserContext`) — EP01-B1-02
- Add EF Core attributes (`[Key]`, `[Required]`) or any persistence-aware annotation
- Implement equality members (`IEquatable<T>`, `Equals`, `GetHashCode`) unless a test requires it
- Create more than 3 exception types total (base + 2 concrete) — consolidate description-length violation into `InvalidTaskTitleException` with a descriptive message, or flag to orchestrator if a third type is genuinely needed

### SCOPE BOUNDARY — Stop when:

- All 7 deliverable files exist (6 created, 1 deleted) and all 10 named tests pass
- All quality gates in Section 6 pass
- Do NOT proceed to Application-layer work (EP01-B1-02)

## 8. Anti-Patterns

| Anti-Pattern                                     | Why It Fails                                                    | Do Instead                                    |
| ------------------------------------------------ | --------------------------------------------------------------- | --------------------------------------------- |
| Using `dueDate.Value < DateTime.UtcNow`          | Makes dueDate==now valid, contradicts AC-004.4 exclusive boundary | Use `<=` — equal-to-now throws                |
| Adding a NuGet package for UUID v7               | `Guid.CreateVersion7()` ships in .NET 10 — no package needed    | Call `Guid.CreateVersion7()` directly          |
| Public setters on TaskItem properties            | Breaks invariant-enforcing factory pattern                       | Private setters, mutated through factory only  |
| Comparing title length before trimming           | Rejects valid titles with leading/trailing whitespace            | Trim first, then measure length (AC-004.8)    |
| Naming collision with `System.Threading.Tasks.TaskStatus` | Ambiguous reference across the solution          | Use fully-qualified name or `using` alias in consuming code |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G4)
2. If G1 (compilation) fails: fix syntax/type errors; check `TaskStatus` enum does not collide with `System.Threading.Tasks.TaskStatus` — use a namespace alias if ambiguous
3. If G2 (unit tests) fails: identify the failing boundary test; re-read Section 5's comparison operators (`<=` for dueDate, `>` for length); the exact boundaries are the historically under-tested spots
4. If G3 (zero PackageReference) fails: remove any accidentally-added package from `.csproj`
5. If G4 fails: remove any extra exception type not listed in Section 5
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in each attempt.

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
TASK: EP01-B1-01
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {any issues or decisions made}
```
