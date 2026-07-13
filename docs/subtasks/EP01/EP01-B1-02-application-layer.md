# Handoff: EP01-B1-02 — Application: CreateTaskCommand, Validator, Handler, Interfaces

## 1. Metadata

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | EP01-B1-02                                |
| Task Name     | Application: CreateTaskCommand, Validator, Handler, Interfaces |
| Batch         | 1 of N (EP01 Chunk-C)                     |
| Epic          | EP01 — Task Management                    |
| User Stories  | US-004 (AC-004.1, AC-004.3, AC-004.4, AC-004.6, AC-004.7, AC-004.8, AC-004.9) |
| Persona       | Vaughn Vernon — Application Layer / Use Cases |
| Model Tier    | sonnet                                    |

## 2. Objective

Define `ITaskRepository` and `ICurrentUserContext` as Application-owned interfaces (Dependency Inversion — concrete implementations arrive in EP01-B1-03a/03b), and implement `CreateTaskCommand`, `CreateTaskCommandValidator` (FluentValidation, assuming global `CascadeMode.Continue`), `CreateTaskCommandHandler`, and `TaskDto`. This task proves the multi-field-violation-together contract (AC-004.6) at the unit-test level via NSubstitute mocks.

## 3. Pre-Conditions

- [ ] EP01-B1-01 reports STATUS: DONE and all its Quality Gates pass
- [ ] `dotnet build src/TaskFlow.Domain/` exits 0
- [ ] `TaskItem.Create(...)`, `TaskStatus`, `FieldLengths` are accessible from `src/TaskFlow.Domain/`
- [ ] `dotnet test tests/TaskFlow.Domain.Tests/ --filter "FullyQualifiedName~TaskItemTests"` exits 0, 10 passed
- [ ] FluentValidation 11.11.0 resolvable on NuGet (if offline, report BLOCKED)
- [ ] NSubstitute 5.3.0 resolvable on NuGet for test project

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

| File                                              | Lines     | Why                                          |
| ------------------------------------------------- | --------- | -------------------------------------------- |
| `docs/user-stories/US-004-create-task.md`         | 13-27     | DOR — CascadeMode.Continue, SeedOwnerId, empty-to-null normalization |
| `docs/user-stories/US-004-create-task.md`         | 29-79     | AC-004.1 through AC-004.10                   |
| `docs/user-stories/US-004-create-task.md`         | 161-171   | Validation Rules — exact semantics           |
| `src/TaskFlow.Domain/Entities/TaskItem.cs`        | all       | Handler calls `TaskItem.Create()` — signature must match |
| `src/TaskFlow.Domain/Constants/FieldLengths.cs`   | all       | Validator reuses these constants              |
| `docs/architecture/clean-architecture.md`         | 145-164   | Application references Domain only           |

## 5. Deliverables

### Files to Create

| File Path                                                                              | Contents |
| -------------------------------------------------------------------------------------- | -------- |
| `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs`                    | Interface: `Guid OwnerId { get; }` |
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs`                        | Interface: `Task AddAsync(TaskItem task, CancellationToken ct)` |
| `src/TaskFlow.Application/Tasks/Dtos/TaskDto.cs`                                       | Record: Id, Title, Description, Status (string), DueDate, OwnerId, CreatedAt, UpdatedAt |
| `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommand.cs`              | Sealed record: `Title`, `Description?`, `DueDate?` — NO Status/Id/OwnerId properties |
| `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommandValidator.cs`     | AbstractValidator using `FieldLengths` constants |
| `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommandHandler.cs`       | Constructs `TaskItem.Create(...)`, calls repo, maps to `TaskDto` |
| `tests/TaskFlow.Application.Tests/Tasks/Commands/CreateTask/CreateTaskCommandValidatorTests.cs` | 6 tests |
| `tests/TaskFlow.Application.Tests/Tasks/Commands/CreateTask/CreateTaskCommandHandlerTests.cs`   | 5 tests |

### Files to Modify

None.

### Expected Signatures

```csharp
// CreateTaskCommandValidator.cs
public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        // NOTE: Do NOT set CascadeMode here. Global default is set in Program.cs (EP01-B1-04a).
        RuleFor(x => x.Title)
            .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("title required")
            .Must(t => t != null && t.Trim().Length <= FieldLengths.TitleMaxLength)
            .WithMessage("title must not exceed 200 characters");

        RuleFor(x => x.DueDate)
            .Must(d => !d.HasValue || d.Value > DateTime.UtcNow)
            .When(x => x.DueDate.HasValue)
            .WithMessage("must be future");

        RuleFor(x => x.Description)
            .Must(d => d == null || d.Length <= FieldLengths.DescriptionMaxLength)
            .WithMessage("description must not exceed 2000 characters");
    }
}

// Test setup pattern (NSubstitute ONLY — no fake implementations):
var currentUser = Substitute.For<ICurrentUserContext>();
currentUser.OwnerId.Returns(Guid.CreateVersion7());
var repo = Substitute.For<ITaskRepository>();
```

**Required Validator Tests (6):**

1. `CreateTaskCommandValidator_WithEmptyTitle_ReturnsValidationError`
2. `CreateTaskCommandValidator_WithNbspOnlyTitle_ReturnsValidationError` — title = `" "`
3. `CreateTaskCommandValidator_WithPastDueDate_ReturnsValidationError`
4. `CreateTaskCommandValidator_WithDescriptionExceeding2000Chars_ReturnsValidationError`
5. `CreateTaskCommandValidator_WithEmptyTitleAndPastDueDate_ReturnsBothErrors` — assert `result.Errors.Count == 2` exactly (CascadeMode.Continue proof for AC-004.6)
6. `CreateTaskCommandValidator_WithValidCommand_NoValidationErrors`

**Required Handler Tests (5):**

1. `CreateTaskCommandHandler_WithValidInput_ReturnsTaskDto`
2. `CreateTaskCommandHandler_AssignsOwnerIdFromCurrentUserContext`
3. `CreateTaskCommandHandler_IgnoresClientSuppliedValues_UsesServerGenerated` — CreateTaskCommand has no Id/OwnerId, so DTO.OwnerId always equals `currentUser.OwnerId`
4. `CreateTaskCommandHandler_SetsCreatedAtEqualToUpdatedAtOnCreation`
5. `CreateTaskCommandHandler_NormalizesEmptyDescriptionToNull`

## 6. Quality Gates

| #  | Gate                          | Command                                                                                                        | Pass Criteria         |
| -- | ----------------------------- | -------------------------------------------------------------------------------------------------------------- | --------------------- |
| G1 | Compilation                   | `dotnet build src/TaskFlow.Application/`                                                                       | exit 0                |
| G2 | Validator unit tests          | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~CreateTaskCommandValidatorTests"`  | exit 0, 6 passed      |
| G3 | Handler unit tests            | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~CreateTaskCommandHandlerTests"`    | exit 0, 5 passed      |
| G4 | CascadeMode proof isolated    | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~WithEmptyTitleAndPastDueDate_ReturnsBothErrors"` | exit 0, 1 passed |
| G5 | Package allowlist             | `dotnet list src/TaskFlow.Application/TaskFlow.Application.csproj package`                                     | only FluentValidation — no EF Core, no Npgsql, no Infrastructure refs |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Implement `ITaskRepository` or `ICurrentUserContext` with any concrete class — Infrastructure (EP01-B1-03a/03b) owns implementations
- Set `CascadeMode.Continue` inside `CreateTaskCommandValidator`'s constructor — the global registration belongs to EP01-B1-04a's Program.cs
- Add EF Core, Npgsql, or any Infrastructure-facing package to `TaskFlow.Application.csproj`
- Hardcode any GUID literal as a "seed owner" — tests use `Substitute.For<ICurrentUserContext>()`
- Create HTTP contracts (`CreateTaskRequest`/`TaskResponse`) — those are API-layer (EP01-B1-04b)

### SCOPE BOUNDARY — Stop when:

- All 8 deliverable files exist and all 11 named tests pass
- All quality gates in Section 6 pass
- Do NOT proceed to Infrastructure or API work

## 8. Anti-Patterns

| Anti-Pattern                                        | Why It Fails                                                 | Do Instead                                     |
| --------------------------------------------------- | ------------------------------------------------------------ | ---------------------------------------------- |
| Writing a fake in-memory `ITaskRepository`          | Blurs Application/Infrastructure boundary                     | `Substitute.For<ITaskRepository>()` only       |
| Setting `CascadeMode.Continue` per-validator        | Hides global config point; future validators silently fail-fast | Assume global default set in Program.cs        |
| Duplicating `FieldLengths.TitleMaxLength` locally   | Drift risk if Domain constants change                         | Import `TaskFlow.Domain.Constants.FieldLengths` |
| Asserting `result.Errors.Any()` instead of `.Count == 2` | Passes even if only 1 violation caught — false confidence | Assert exact count                             |
| Naming test `_AlwaysAssignsSeedOwnerId`             | Bleeds Delivery-1 shim naming into a test that outlives it    | Use `_AssignsOwnerIdFromCurrentUserContext`     |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G5)
2. If G1 fails: check `CreateTaskCommand`/`TaskDto` compile against `TaskItem`'s actual public API
3. If G2 fails on combined-error test: FluentValidation's `TestValidate` respects `ValidatorOptions.Global` — add a one-time `ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue;` in test fixture setup (this does NOT replace the Program.cs requirement, it lets this task's own tests prove the validator shape is Continue-compatible)
4. If G3 fails: check NSubstitute mock setup — common miss is forgetting `.Returns(...)` on `currentUser.OwnerId`
5. If G5 fails: remove any accidentally-added package reference
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and report FAILED with: (a) which gate, (b) full error output, (c) what was tried.

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
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B1-02
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {any issues or decisions made}
```
