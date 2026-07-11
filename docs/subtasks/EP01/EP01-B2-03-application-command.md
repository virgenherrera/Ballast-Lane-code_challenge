# Handoff: EP01-B2-03 — Application: UpdateTaskCommand, Handler, and Validator

## 1. Metadata

| Field        | Value                                                                        |
| ------------ | ---------------------------------------------------------------------------- |
| Task ID      | EP01-B2-03                                                                   |
| Task Name    | Application: UpdateTaskCommand, UpdateTaskCommandHandler, UpdateTaskCommandValidator |
| Batch        | 2 of N (EP01 Chunk-U)                                                        |
| Epic         | EP01 — Task Management                                                       |
| User Stories | US-007 (AC-007.1, AC-007.2, AC-007.3, AC-007.4, AC-007.5, AC-007.6, AC-007.7) |
| Persona      | Uncle Bob — Application Layer / CQRS                                         |
| Model Tier   | sonnet                                                                       |

## 2. Objective

Create the `UpdateTaskCommand` record (all updatable fields optional for PATCH semantics), `UpdateTaskCommandHandler` (fetches via `GetByIdAsync`, enforces ownership via `TaskOwnershipSpecification.EnsureOwnedBy`, calls only domain update methods for non-null fields, persists via EF Core change tracking), and `UpdateTaskCommandValidator` (a FULLY INDEPENDENT `AbstractValidator<UpdateTaskCommand>` with `CascadeMode.Continue` — ZERO shared base class with `CreateTaskCommandValidator` — including a cross-property "at least one field" rule for AC-007.7).

**CRITICAL ANTI-PATTERN**: `UpdateTaskCommandValidator` must NEVER inherit from, share a base class with, or share a rule object with `CreateTaskCommandValidator`. This is the story's highest-rated risk — a shared validator would silently reintroduce past-date rejection on Update.

## 3. Pre-Conditions

- [ ] EP01-B2-01 STATUS: DONE — `TaskItem.Rename/ChangeStatus/UpdateDescription/Reschedule` exist
- [ ] EP01-B2-02 STATUS: DONE — `ITaskRepository.GetByIdAsync`, `TaskNotFoundException`, `TaskOwnershipSpecification` exist
- [ ] `dotnet build` exits 0
- [ ] No directory `src/TaskFlow.Application/Tasks/Commands/UpdateTask/` exists
- [ ] `CreateTaskCommandValidator` is confirmed to be `sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>` with no intermediate base class

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `src/TaskFlow.Domain/Entities/TaskItem.cs` | full (post B2-01) | Exact signatures of the four update methods |
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` | full (post B2-02) | `AddAsync` + `GetByIdAsync` |
| `src/TaskFlow.Application/Common/Exceptions/TaskNotFoundException.cs` | full (post B2-02) | Exception thrown on ownership failure |
| `src/TaskFlow.Application/Common/Specifications/TaskOwnershipSpecification.cs` | full (post B2-02) | Helper to call — MUST be reused |
| `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs` | 1-6 | `OwnerId` for ownership check |
| `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommand.cs` | 1-6 | Pattern reference ONLY |
| `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommandValidator.cs` | 1-23 | Read to confirm no shared base; do NOT reference from new validator |
| `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommandHandler.cs` | 1-46 | Handler pattern reference |
| `src/TaskFlow.Application/Tasks/Dtos/TaskDto.cs` | 1-11 | Return shape |
| `tests/TaskFlow.Application.Tests/Tasks/Commands/CreateTask/CreateTaskCommandValidatorTests.cs` | 1-80 | Test style reference — note `ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue` in constructor |
| `docs/user-stories/US-007-update-task.md` | 28-66 | Full AC set |
| `docs/user-stories/US-007-update-task.md` | 100-122 | Test Plan with exact test names |
| `docs/user-stories/US-007-update-task.md` | 136-138 | CRITICAL Risk: shared validator regression |

## 5. Deliverables

### Files to Create

| File Path | Contents |
|-----------|----------|
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommand.cs` | Sealed record with all-optional fields |
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandHandler.cs` | Handler: fetch, ownership check, conditional mutation, persist |
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandValidator.cs` | Independent validator with CascadeMode.Continue |
| `tests/TaskFlow.Application.Tests/Tasks/Commands/UpdateTask/UpdateTaskCommandHandlerTests.cs` | Handler unit tests (mocked ITaskRepository) |
| `tests/TaskFlow.Application.Tests/Tasks/Commands/UpdateTask/UpdateTaskCommandValidatorTests.cs` | Validator unit tests |

### Files to Modify

None — the shared contracts from EP01-B2-02 must NOT be touched.

### Expected Signatures

```csharp
// UpdateTaskCommand.cs
namespace TaskFlow.Application.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand(
    Guid Id,
    string? Title,
    string? Description,
    string? Status,
    DateTime? DueDate);
// PATCH semantics: null == "do not touch" for all fields except Id.
// For Delivery 1, explicit JSON null and omission are both treated identically as null here.
```

```csharp
// UpdateTaskCommandHandler.cs
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Specifications;
using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserContext currentUserContext)
    {
        _taskRepository = taskRepository;
        _currentUserContext = currentUserContext;
    }

    public async System.Threading.Tasks.Task<TaskDto> Handle(
        UpdateTaskCommand command,
        CancellationToken ct)
    {
        var existing = await _taskRepository.GetByIdAsync(command.Id, ct);
        // Ownership check BEFORE any mutation — throws TaskNotFoundException for both
        // non-existent and owner-mismatch (AC-007.5). SaveChangesAsync is never reached.
        var task = TaskOwnershipSpecification.EnsureOwnedBy(
            existing, _currentUserContext.OwnerId, command.Id);

        if (command.Title is not null)
        {
            task.Rename(command.Title);
        }

        if (command.Description is not null)
        {
            task.UpdateDescription(command.Description);
        }

        if (command.Status is not null)
        {
            var parsed = Enum.Parse<Domain.Enums.TaskStatus>(
                command.Status.Replace(" ", string.Empty), ignoreCase: true);
            task.ChangeStatus(parsed);
        }

        if (command.DueDate.HasValue)
        {
            task.Reschedule(command.DueDate);
        }

        // EF Core change tracking: the entity fetched via GetByIdAsync is already tracked.
        // Calling SaveChangesAsync persists all mutations made via domain methods.
        // No need to call AddAsync — the entity already exists in the DB.
        // NOTE: SaveChangesAsync must be available. If ITaskRepository doesn't expose it,
        // add a SaveChangesAsync method to ITaskRepository here (see Boundaries note).
        // For now, assume the repository or DbContext exposes this capability.

        return new TaskDto(
            task.Id,
            task.Title,
            task.Description,
            task.Status.ToString(),
            task.DueDate,
            task.OwnerId,
            task.CreatedAt,
            task.UpdatedAt);
    }
}
```

```csharp
// UpdateTaskCommandValidator.cs
// CRITICAL: ZERO shared base class, ZERO shared rule objects with CreateTaskCommandValidator.
// CreateTaskCommandValidator is sealed AbstractValidator<CreateTaskCommand> — this one must be
// equally standalone. Do NOT create a shared abstract validator base.
using FluentValidation;
using TaskFlow.Domain.Constants;

namespace TaskFlow.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    private static readonly string[] ValidStatuses = ["Pending", "In Progress", "Completed"];

    public UpdateTaskCommandValidator()
    {
        // Presence-aware rules: only validate when the field is actually provided (non-null).
        // A null field means "do not touch" — must NOT trigger validation failure.
        RuleFor(x => x.Title)
            .Must(t => !string.IsNullOrWhiteSpace(t))
            .When(x => x.Title is not null)
            .WithMessage("title required");

        RuleFor(x => x.Title)
            .Must(t => t != null && t.Trim().Length <= FieldLengths.TitleMaxLength)
            .When(x => x.Title is not null)
            .WithMessage($"title must not exceed {FieldLengths.TitleMaxLength} characters");

        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .When(x => x.Status is not null)
            .WithMessage($"status must be one of: {string.Join(", ", ValidStatuses)}");

        RuleFor(x => x.Description)
            .Must(d => d!.Length <= FieldLengths.DescriptionMaxLength)
            .When(x => x.Description is not null)
            .WithMessage($"description must not exceed {FieldLengths.DescriptionMaxLength} characters");

        // AC-007.7: at least one updatable field must be present.
        // Cross-property rule spanning the whole command — NOT foldable into per-field rules.
        RuleFor(x => x)
            .Must(cmd => cmd.Title is not null || cmd.Description is not null
                       || cmd.Status is not null || cmd.DueDate.HasValue)
            .WithMessage("at least one field is required")
            .WithName("payload");

        // NO RuleFor on DueDate rejecting past dates — Update explicitly ALLOWS them (AC-007.3).
    }
}
```

**Persistence note**: `ITaskRepository` currently has only `AddAsync` and `GetByIdAsync`. Since the fetched entity is EF-Core-tracked, mutations are persisted by calling `SaveChangesAsync` on the DbContext. Add a `Task SaveChangesAsync(CancellationToken ct)` method to `ITaskRepository` and implement it in `TaskRepository` — this is the minimal, least-invasive extension needed for the handler to persist changes without re-adding the entity.

### Required Test Names

*Handler tests* (`UpdateTaskCommandHandlerTests.cs`):
1. `UpdateTaskCommandHandler_ForTaskOwnedByAnotherUser_ThrowsNotFound` — AC-007.5; assert `SaveChangesAsync` was NEVER called (`Received(0)`)
2. `UpdateTaskCommandHandler_WithPartialFields_OnlyUpdatesSpecifiedFieldsPreservingRest` — AC-007.1, AC-007.3; assert ALL FOUR fields: the one changed AND three unchanged
3. `UpdateTaskCommandHandler_WithPastDueDate_SucceedsWithoutValidationError` — AC-007.3

*Validator tests* (`UpdateTaskCommandValidatorTests.cs`):
4. `UpdateTaskCommandValidator_WithInvalidStatusString_FailsWithValidValuesListed` — AC-007.4
5. `UpdateTaskCommandValidator_WithEmptyTitleAndInvalidStatus_FailsWithBothErrorsInDetails` — AC-007.4 + AC-007.6; CascadeMode.Continue proof
6. `UpdateTaskCommandValidator_WithNoFieldsProvided_Fails` — AC-007.7
7. `UpdateTaskCommandValidator_WithPastDueDate_DoesNotFail` — AC-007.3; validator-level asymmetry proof

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Compilation | `dotnet build` | exit 0 |
| G2 | Handler tests | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~UpdateTaskCommandHandlerTests"` | exit 0, 3 passed |
| G3 | Validator tests | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~UpdateTaskCommandValidatorTests"` | exit 0, 4 passed |
| G4 | CascadeMode proof isolated | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~WithEmptyTitleAndInvalidStatus_FailsWithBothErrorsInDetails"` | exit 0, 1 passed, asserts `Errors.Count >= 2` |
| G5 | Zero shared base | Verify `UpdateTaskCommandValidator` inherits directly from `AbstractValidator<UpdateTaskCommand>` — no intermediate class | code review |
| G6 | Ownership helper reused | Grep: `UpdateTaskCommandHandler.cs` calls `TaskOwnershipSpecification.EnsureOwnedBy` and contains NO inline `if (task.OwnerId != ...)` | verified |
| G7 | Regression — Create suite | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~CreateTaskCommand"` | exit 0, all existing tests pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Modify `CreateTaskCommandValidator.cs` or `CreateTaskCommand.cs` or `CreateTaskCommandHandler.cs`
- Create a shared base class for Create/Update validators
- Implement the API controller, DTO, or middleware — EP01-B2-04
- Add a dueDate past-date rejection rule to `UpdateTaskCommandValidator` — the absence IS the feature
- Handle explicit-null-clears-field semantics (deferred per DOR; null == "do not touch")
- Add title max-length from within this validator unless the field is provided (conditional rule with `.When`)

### SCOPE BOUNDARY — Stop when:

- All 7 named tests pass, G4 isolated CascadeMode proof passes
- G5 and G6 verified
- All quality gates green
- Do NOT proceed to API-layer work

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| `UpdateTaskCommandValidator : CreateTaskCommandValidator` or shared abstract base | Reintroduces past-date rejection — CRITICAL regression | Fully independent `AbstractValidator<UpdateTaskCommand>` |
| `RuleFor(x => x.Title).NotEmpty()` without `.When(x => x.Title is not null)` | Rejects omitted title (null) as "empty" — but omission must be valid | Presence-aware conditional: only validate non-null values |
| Folding AC-007.7 into per-field rules | Each field's rule passes individually on empty object | One cross-property `RuleFor(x => x)` rule spanning all fields |
| Reimplementing ownership check inline | Diverges from the single source of truth (EP01-B2-02) | Call `TaskOwnershipSpecification.EnsureOwnedBy` |
| Calling any mutation method before the ownership check | Could mutate a task the caller does not own | Ownership check MUST happen immediately after fetch, before any `.Rename()/.ChangeStatus()` etc. |
| Using `_taskRepository.AddAsync(task, ct)` to persist an update | Attempting to add an already-tracked entity throws EF Core duplicate key | Use `SaveChangesAsync` — entity is already tracked from `GetByIdAsync` |

## 9. Rollback Guidance

1. If G4 fails: set `CascadeMode.Continue` explicitly per rule or set `ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue` in test constructor (matching existing Create test pattern)
2. If G5 fails: remove any intermediate base class
3. If G6 fails: replace inline ownership `if` with `TaskOwnershipSpecification.EnsureOwnedBy`
4. If handler cannot persist (no `SaveChangesAsync` available): add `Task SaveChangesAsync(CancellationToken ct)` to `ITaskRepository` and implement in `TaskRepository` — this is an acceptable scope extension for this task
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

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
- CRITICAL: no shared validator inheritance between Create and Update

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17.5, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B2-03
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G5/G6 verified; note if SaveChangesAsync was added to ITaskRepository; confirm no shared validator inheritance}
```
