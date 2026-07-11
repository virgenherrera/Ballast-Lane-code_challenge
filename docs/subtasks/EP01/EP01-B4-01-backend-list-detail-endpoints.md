# Handoff: EP01-B4-01 — Backend: List (Paginated/Filtered) + Detail Read Endpoints

## 1. Metadata

| Field        | Value                                                                          |
| ------------ | ------------------------------------------------------------------------------ |
| Task ID      | EP01-B4-01                                                                     |
| Task Name    | Backend: GET /api/tasks (paginated list + status filter) + GET /api/tasks/{id} |
| Batch        | 4 of 4 (EP01 Chunk-R — Read Operations)                                       |
| Epic         | EP01 — Task Management                                                         |
| User Stories | US-005 (AC-005.1–005.12), US-006 (AC-006.1–006.6), US-009 (AC-009.1–009.5)    |
| Persona      | Uncle Bob — Clean Architecture + Kent C. Dodds — Testing                       |
| Model Tier   | sonnet                                                                         |

## 2. Objective

Replace the TEMPORARY `GetAll` action and `GetAllAsync` repository method with two production GET endpoints on `TasksController`: (1) a paginated, status-filterable list endpoint (`GET /api/tasks`) returning `{items, paging}` with ownership isolation, deterministic ordering, and prev/next link generation; (2) a detail endpoint (`GET /api/tasks/{id}`) returning the full 8-field task representation with anti-enumeration 404 parity. Register all new handlers/validators in DI. Write unit tests (NSubstitute) for handlers, validators, and `PagingLinkBuilder`, plus integration tests (Testcontainers PostgreSQL) covering all 30 acceptance criteria across the three stories' backend scope.

## 3. Pre-Conditions

- [ ] `dotnet build` exits 0
- [ ] `src/TaskFlow.API/Controllers/TasksController.cs` exists with TEMPORARY `GetAll` action (lines 38-57)
- [ ] `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` exists with TEMPORARY `GetAllAsync` (line 11)
- [ ] `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs` contains `SeedOwnerId` and `SeedOwnerId2`
- [ ] `src/TaskFlow.Application/Common/Specifications/TaskOwnershipSpecification.cs` exists with `EnsureOwnedBy`
- [ ] Docker daemon is running (required for Testcontainers)

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `src/TaskFlow.API/Controllers/TasksController.cs` | all | Replace TEMPORARY `GetAll`, add list + detail actions |
| `src/TaskFlow.API/Program.cs` | 42-87 | DI registrations + options pattern for `PaginationOptions` |
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` | all | Replace TEMPORARY `GetAllAsync`, add `ListAsync` |
| `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs` | all | Ownership context interface |
| `src/TaskFlow.Application/Common/Specifications/TaskOwnershipSpecification.cs` | all | Reuse for detail endpoint ownership check |
| `src/TaskFlow.Application/Common/Exceptions/TaskNotFoundException.cs` | all | Exception mapped to 404 |
| `src/TaskFlow.Application/Tasks/Dtos/TaskDto.cs` | all | Reuse as detail response DTO (8 fields) |
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandHandler.cs` | 25-31 | Reference: `GetByIdAsync` + `EnsureOwnedBy` pattern |
| `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandValidator.cs` | all | Reference: FluentValidation pattern |
| `src/TaskFlow.Domain/Entities/TaskItem.cs` | 1-18 | Entity shape (Id, Title, Status, DueDate, OwnerId, CreatedAt, UpdatedAt) |
| `src/TaskFlow.Domain/Enums/TaskStatus.cs` | all | Enum: `Pending`, `InProgress`, `Completed` — note `InProgress` has no space |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | all | Replace TEMPORARY `GetAllAsync`, add `ListAsync` |
| `src/TaskFlow.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs` | all | EF config — add composite index |
| `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs` | all | Seed owner IDs for tests |
| `src/TaskFlow.API/Contracts/TaskResponse.cs` | all | Reference: existing 8-field API contract record |
| `src/TaskFlow.API/Middleware/ValidationExceptionHandler.cs` | all | Existing 400 mapper |
| `src/TaskFlow.API/Middleware/TaskNotFoundExceptionHandler.cs` | all | Existing 404 mapper |
| `tests/TaskFlow.IntegrationTests/Common/IntegrationTestBase.cs` | all | Test base class |
| `tests/TaskFlow.IntegrationTests/Common/AssertErrorResponse.cs` | all | Shared 400 assertion helper |
| `tests/TaskFlow.IntegrationTests/Tasks/DeleteTaskEndpointTests.cs` | 1-60 | Test style reference: helpers, `SeedTaskDirectlyAsync` |
| `tests/TaskFlow.Application.Tests/Tasks/Commands/UpdateTask/UpdateTaskCommandHandlerTests.cs` | 1-18 | Unit test pattern: NSubstitute setup |
| `docs/architecture/api-contract.md` | 260-575 | Sections 4.2, 4.3, 7, 8 — list/detail/filter/pagination contracts |

## 5. Deliverables

### Files to Create

| File Path | Contents |
|-----------|----------|
| `src/TaskFlow.Application/Tasks/Queries/ListTasks/ListTasksQuery.cs` | Query record: `Guid OwnerId, string? Status, int? Page, int? PerPage` |
| `src/TaskFlow.Application/Tasks/Queries/ListTasks/ListTasksQueryHandler.cs` | Handler: validates, calls `ListAsync`, builds paging links via `PagingLinkBuilder`, returns `ListTasksResult` |
| `src/TaskFlow.Application/Tasks/Queries/ListTasks/ListTasksQueryValidator.cs` | FluentValidation: page >= 1, perPage 1-100, status enum check — `CascadeMode.Continue` is global, do NOT set per-validator |
| `src/TaskFlow.Application/Tasks/Queries/ListTasks/ListTasksResult.cs` | Result record: `IReadOnlyList<TaskListItemDto> Items, PagingInfo Paging` |
| `src/TaskFlow.Application/Tasks/Queries/ListTasks/TaskListItemDto.cs` | DTO record: `Guid Id, string Title, string Status, DateTime? DueDate` (4 fields only) |
| `src/TaskFlow.Application/Tasks/Queries/ListTasks/PagingInfo.cs` | Record: `int Page, int PerPage, int Total, string? Prev, string? Next` |
| `src/TaskFlow.Application/Tasks/Queries/GetTaskById/GetTaskByIdQuery.cs` | Query record: `Guid TaskId, Guid OwnerId` |
| `src/TaskFlow.Application/Tasks/Queries/GetTaskById/GetTaskByIdQueryHandler.cs` | Handler: calls `GetByIdAsync` + `TaskOwnershipSpecification.EnsureOwnedBy`, returns `TaskDto` |
| `src/TaskFlow.Application/Common/Mapping/TaskStatusMapper.cs` | Static helper: `ToDisplayString(TaskStatus) -> string` and `ParseOrNull(string?) -> TaskStatus?` — single source of truth for `InProgress` to `"In Progress"` mapping |
| `src/TaskFlow.Application/Common/Pagination/PaginationDefaults.cs` | Static class: `DefaultPage = 1`, `DefaultPerPage = 20`, `MaxPerPage = 100` |
| `src/TaskFlow.Application/Common/Pagination/PagingLinkBuilder.cs` | Static helper: builds relative prev/next URLs preserving status filter, canonical param order |
| `src/TaskFlow.API/Contracts/TaskListResponse.cs` | API contract: `IReadOnlyList<TaskListItemResponse> Items, PagingResponse Paging` |
| `src/TaskFlow.API/Contracts/TaskListItemResponse.cs` | API contract record: `Guid Id, string Title, string Status, DateTime? DueDate` |
| `src/TaskFlow.API/Contracts/PagingResponse.cs` | API contract record: `int Page, int PerPage, int Total, string? Prev, string? Next` |
| `tests/TaskFlow.Application.Tests/Tasks/Queries/ListTasks/ListTasksQueryHandlerTests.cs` | Unit tests: handler logic with mocked repository |
| `tests/TaskFlow.Application.Tests/Tasks/Queries/ListTasks/ListTasksQueryValidatorTests.cs` | Unit tests: validator rules (page, perPage, status) |
| `tests/TaskFlow.Application.Tests/Tasks/Queries/GetTaskById/GetTaskByIdQueryHandlerTests.cs` | Unit tests: owned task returns DTO, not-found throws, cross-owner throws |
| `tests/TaskFlow.Application.Tests/Common/Pagination/PagingLinkBuilderTests.cs` | Unit tests: prev/next URL generation, filter preservation, null at boundaries |
| `tests/TaskFlow.IntegrationTests/Tasks/ListTasksEndpointTests.cs` | Integration tests: all list + filter ACs against real PostgreSQL |
| `tests/TaskFlow.IntegrationTests/Tasks/GetTaskByIdEndpointTests.cs` | Integration tests: all detail ACs against real PostgreSQL |

### Files to Modify

| File Path | Change |
|-----------|--------|
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` | Remove TEMPORARY `GetAllAsync`; add `ListAsync(Guid ownerId, TaskStatus? status, int page, int perPage, CancellationToken ct)` returning `Task<(IReadOnlyList<TaskItem> Items, int Total)>` |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | Remove TEMPORARY `GetAllAsync` impl; add `ListAsync` with conditional status filter (C# branching, NOT `||` in LINQ), `OrderByDescending(CreatedAt).ThenByDescending(Id)`, `Skip/Take`, `CountAsync` |
| `src/TaskFlow.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs` | Add composite index: `builder.HasIndex(x => new { x.OwnerId, x.CreatedAt, x.Id }).HasDatabaseName("IX_tasks_owner_id_created_at_id").IsDescending(false, true, true)` |
| `src/TaskFlow.API/Controllers/TasksController.cs` | Remove TEMPORARY `GetAll` action + `_taskRepository` field/constructor param; add `ListTasksQueryHandler`, `ListTasksQueryValidator`, `GetTaskByIdQueryHandler` to constructor; add `GetList` and `GetById` actions |
| `src/TaskFlow.API/Program.cs` | Add DI registrations: `ListTasksQueryHandler`, `ListTasksQueryValidator`, `GetTaskByIdQueryHandler`; add `PaginationOptions` binding with `ValidateOnStart` |

### Expected Signatures

```csharp
// ITaskRepository — replace GetAllAsync with:
Task<(IReadOnlyList<TaskItem> Items, int Total)> ListAsync(
    Guid ownerId, Domain.Enums.TaskStatus? status,
    int page, int perPage, CancellationToken ct);

// Keep existing:
Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct);
```

```csharp
// ListTasksQuery
public sealed record ListTasksQuery(
    Guid OwnerId, string? Status, int? Page, int? PerPage);
```

```csharp
// GetTaskByIdQuery
public sealed record GetTaskByIdQuery(Guid TaskId, Guid OwnerId);
```

```csharp
// PagingLinkBuilder — static, pure function
public static class PagingLinkBuilder
{
    public static (string? Prev, string? Next) Build(
        int page, int perPage, int total, string? status);
}
// Canonical param order in URLs: page, perPage, status (alphabetical)
// Example: /api/tasks?page=1&perPage=20&status=Pending
```

```csharp
// PaginationDefaults — literal constants, not configuration
public static class PaginationDefaults
{
    public const int DefaultPage = 1;
    public const int DefaultPerPage = 20;
    public const int MaxPerPage = 100;
}
```

```csharp
// TasksController — new list action (replaces GetAll):
[HttpGet]
[ProducesResponseType(typeof(TaskListResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> GetList(
    [FromQuery] string? status,
    [FromQuery] int? page,
    [FromQuery] int? perPage,
    CancellationToken ct)

// TasksController — new detail action:
[HttpGet("{id}")]
[ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetById(string id, CancellationToken ct)
// Uses string id + Guid.TryParse — same pattern as Update/Delete
```

### FROZEN DECISIONS (sub-agent MUST follow, no discretion)

**1. TaskStatus `InProgress` to `"In Progress"` mapping**: The C# enum member is `InProgress` (no space). The API contract requires `"In Progress"` (with space). Use the SAME manual switch-expression pattern already established in `UpdateTaskCommandHandler.ParseStatus` (lines 66-72). For serialization (enum to string), use a static helper method in the Application layer:

```csharp
// In a shared location, e.g. TaskStatusMapper.cs in Application/Common/
public static class TaskStatusMapper
{
    public static string ToDisplayString(Domain.Enums.TaskStatus status) => status switch
    {
        Domain.Enums.TaskStatus.Pending => "Pending",
        Domain.Enums.TaskStatus.InProgress => "In Progress",
        Domain.Enums.TaskStatus.Completed => "Completed",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static Domain.Enums.TaskStatus? ParseOrNull(string? value) => value switch
    {
        null => null,
        "Pending" => Domain.Enums.TaskStatus.Pending,
        "In Progress" => Domain.Enums.TaskStatus.InProgress,
        "Completed" => Domain.Enums.TaskStatus.Completed,
        _ => null // validator catches invalid values before this is called
    };
}
```

Also create `src/TaskFlow.Application/Common/Mapping/TaskStatusMapper.cs` and add it to Files to Create.

**2. US-006 repository method**: REUSE the existing `GetByIdAsync(Guid id, CancellationToken ct)` + `TaskOwnershipSpecification.EnsureOwnedBy` pattern. This is the same pattern used by `UpdateTaskCommandHandler` (lines 25-30). Do NOT create a new `GetByIdForOwnerAsync` method. The `EnsureOwnedBy` spec throws `TaskNotFoundException` for both null (not found) and wrong owner, producing identical 404 responses via `TaskNotFoundExceptionHandler`.

**3. TaskDetailDto**: REUSE the existing `TaskDto` record (`src/TaskFlow.Application/Tasks/Dtos/TaskDto.cs`) as the detail response. Do NOT create a new `TaskDetailDto` — `TaskDto` already has the exact 8-field shape required (Id, Title, Description, Status, DueDate, OwnerId, CreatedAt, UpdatedAt).

**4. Repository filter pattern**: Build `IQueryable` conditionally (C# `if` branching), NOT with `status == null || t.Status == status` inside a single `.Where()`. The `OR` pattern can inhibit index usage in PostgreSQL query plans.

```csharp
// CORRECT — conditional LINQ composition:
var query = _dbContext.Tasks.AsNoTracking().Where(t => t.OwnerId == ownerId);
if (status.HasValue)
    query = query.Where(t => t.Status == status.Value);
// then OrderBy, Count, Skip, Take

// WRONG — OR inside Where:
// query.Where(t => t.OwnerId == ownerId && (status == null || t.Status == status))
```

**5. Prev/next URL format**: Relative paths (no scheme/host). Canonical query parameter order: `page`, `perPage`, `status`. Example: `/api/tasks?page=2&perPage=20&status=Pending`. Use `Uri.EscapeDataString` for the status value to handle the space in `"In Progress"` consistently.

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Compilation | `dotnet build` | exit 0 |
| G2 | Unit tests — list handler | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~ListTasksQueryHandlerTests"` | exit 0, 3+ tests passed |
| G3 | Unit tests — list validator | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~ListTasksQueryValidatorTests"` | exit 0, 5+ tests passed |
| G4 | Unit tests — detail handler | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~GetTaskByIdQueryHandlerTests"` | exit 0, 3+ tests passed |
| G5 | Unit tests — paging links | `dotnet test tests/TaskFlow.Application.Tests/ --filter "FullyQualifiedName~PagingLinkBuilderTests"` | exit 0, 5+ tests passed |
| G6 | Integration — list+filter | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~ListTasksEndpointTests"` | exit 0, 15+ tests passed |
| G7 | Integration — detail | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~GetTaskByIdEndpointTests"` | exit 0, 8+ tests passed |
| G8 | Route uses string id | Code review: `[HttpGet("{id}")]` with `string id` parameter, NOT `Guid id` or `{id:guid}` | verified |
| G9 | TEMPORARY code removed | `grep -rn "TEMPORARY" src/TaskFlow.API/Controllers/TasksController.cs src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | 0 matches |
| G10 | DI registered | `grep -n "ListTasksQueryHandler\|ListTasksQueryValidator\|GetTaskByIdQueryHandler" src/TaskFlow.API/Program.cs` | 3+ matches |
| G11 | No EF/Npgsql in Domain/App | `grep -rn "using Microsoft.EntityFrameworkCore\|using Npgsql" src/TaskFlow.Domain/ src/TaskFlow.Application/` | 0 matches |
| G12 | Regression — all tests | `dotnet test` | exit 0, all existing tests pass |
| G13 | Format check | `dotnet format --verify-no-changes` | exit 0 |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Add JWT authentication, `[Authorize]`, or token validation — Delivery 3 concern
- Implement sorting other than `createdAt DESC, id DESC` — no `sortBy`/`sortOrder` params
- Add full-text search, `q` param, or title filtering
- Support multi-value status filter (e.g., `status=Pending,Completed`)
- Implement case-insensitive or fuzzy status matching — exact match only
- Add cursor-based pagination or jump-to-page numbered links
- Create soft-delete filtering or global query filters
- Add rate limiting
- Modify existing Create, Update, or Delete actions/handlers
- Add NuGet packages not already in the solution
- Add any `<input>` or additional filter beyond the `status` query param
- Add `sortBy`, `search`, `q`, or any query params beyond `status`, `page`, `perPage` "for flexibility" — the contract is exactly these 3 params

### SCOPE BOUNDARY — Stop when:

- Both GET endpoints return correct response shapes per api-contract.md sections 4.2 and 4.3
- All quality gates pass
- Do NOT proceed to frontend or e2e work

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| `[HttpGet("{id:guid}")]` route constraint | Yields 404 for malformed GUID via ASP.NET routing instead of controller-level 400 | `string id` + `Guid.TryParse` — consistent with Update/Delete |
| `JsonStringEnumConverter` on `TaskStatus` | Emits `"InProgress"` (no space), not `"In Progress"` (with space) per API contract | Manual `TaskStatusMapper` switch-expression (see Frozen Decisions) |
| `Where(t => status == null \|\| t.Status == status)` in LINQ | OR pattern inhibits PostgreSQL index usage | Conditional `if (status.HasValue)` LINQ composition |
| `.ToList()` before `.Skip()/.Take()` | Loads ALL rows into memory before paginating — corrupts `paging.total` and performance | Single LINQ chain: `Where` -> `OrderBy` -> `Skip` -> `Take` -> `ToListAsync` |
| Setting `CascadeMode` per-validator | Overrides global setting from Program.cs; inconsistent behavior | `CascadeMode.Continue` is already global (Program.cs line 75) — do not set again |
| Creating `GetByIdForOwnerAsync` in repository | Adds new repository surface; deviates from Update/Delete pattern (`GetByIdAsync` + `EnsureOwnedBy`) | Reuse `GetByIdAsync` + `TaskOwnershipSpecification.EnsureOwnedBy` |
| Creating a new `TaskDetailDto` record | Duplicates the existing `TaskDto` (8 identical fields); drift risk | Reuse `TaskDto` for detail response |
| Calling `Enum.Parse<TaskStatus>(statusString)` inside LINQ `Where` | EF Core cannot translate `Enum.Parse` to SQL — runtime failure, not compile-time | Convert string to enum BEFORE building the `IQueryable` (in handler, using `TaskStatusMapper.ParseOrNull`) |
| Hardcoding `DefaultPerPage` in multiple places | Drift risk if value changes | Use `PaginationDefaults.DefaultPerPage` constant everywhere |

## 9. Rollback Guidance

1. If G1 fails: check constructor injection order in `TasksController` — ensure all new handler parameters are added and field-assigned
2. If G2-G5 fail: verify NSubstitute mock setup matches the new `ListAsync` / `GetByIdAsync` signatures
3. If G6 fails on Testcontainers startup: verify Docker is running and the credential workaround is in place (scratch `DOCKER_CONFIG` with `credsStore` omitted)
4. If G6 fails on filter tests: verify `TaskStatusMapper` handles `"In Progress"` (with space) correctly in both directions; check that the validator rejects case-mismatched values
5. If G7 fails on cross-owner 404: verify `TaskOwnershipSpecification.EnsureOwnedBy` is called with correct `OwnerId` from `ICurrentUserContext`, and `TaskNotFoundExceptionHandler` produces identical body
6. If G9 fails: ensure the TEMPORARY `GetAll` action and `GetAllAsync` method/implementation are fully removed, not just commented out
7. If G11 fails: ensure no EF Core `using` statements leaked into Domain or Application projects
8. If G12 fails on existing tests: removing `GetAllAsync` may break any test that calls it directly — check `TaskRepositoryTests` for usages and update if needed
9. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- Integration tests at API level (AAA pattern) are the PRIMARY confidence layer
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Tests map directly to user story acceptance criteria
- PostgreSQL via Testcontainers (postgres:17.5) — never InMemory/SQLite

### TASKFLOW-ANTI-DRIFT
- Every decision must trace back to a requirement or AC
- Route parameter: `string id`, NOT `Guid id`, NOT `{id:guid}` — frozen decision
- All 404 responses use the same standard error shape via TaskNotFoundExceptionHandler
- All 400 responses use the same standard error shape via ValidationExceptionHandler
- `TaskStatus` enum member `InProgress` maps to wire string `"In Progress"` via `TaskStatusMapper` — never via `JsonStringEnumConverter`

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no EF Core InMemory, no SQLite
- Docker Compose: postgres:17.5, taskflow-api, taskflow-web
- Docker credential workaround: scratch DOCKER_CONFIG with credsStore omitted

### TASKFLOW-PAGINATION
- `PaginationDefaults.DefaultPerPage = 20`, `MaxPerPage = 100` — literal constants
- Prev/next URLs are relative paths, canonical param order: `page`, `perPage`, `status`
- `paging.total` reflects filtered count (ownership + status filter applied)
- Page beyond total returns `items: []` with correct `paging.total`, HTTP 200 — never an error
- Ordering: `createdAt DESC, id DESC` — deterministic tie-breaker on timestamp collision

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B4-01
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G8 string id route; confirm G9 TEMPORARY removed; confirm G10 DI registered; confirm G11 no EF leakage; confirm byte-identical 404 for cross-owner vs not-found}
```
