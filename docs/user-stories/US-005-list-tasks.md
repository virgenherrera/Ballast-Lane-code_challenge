> [📚 INDEX](../INDEX.md) / [EP01 — Task Management](../epics/EP01-task-management.md) / US-005

# US-005 — List Tasks

**Epic**: [EP01 - Task Management](../epics/EP01-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **see a list of all my tasks** so that **I can have an overview of my work**.

## Definition of Ready

- [ ] API contract for GET /api/tasks frozen per docs/architecture/api-contract.md Section 4.2 and Section 8: query params (status, page, perPage), response shape { items: [{id, title, status, dueDate}], paging: {page, perPage, total, prev, next} }
- [ ] Ordering rule confirmed: createdAt DESC with id DESC as deterministic tie-breaker for millisecond-collision stability across page boundaries
- [ ] Server-side pagination defaults must be pinned to literal numbers before implementation: DefaultPerPage (e.g., 20) and MaxPerPage (100) defined as PaginationDefaults constants and bound via PaginationOptions from appsettings.json with ValidateOnStart
- [ ] ICurrentUserContext port (Application layer) agreed: exposes Guid OwnerId; Delivery 1 implementation is HardcodedCurrentUserContext returning a fixed seeded UUID v7 constant (SeedData.DefaultOwnerId)
- [ ] Seed data includes at least 2 distinct ownerId UUID v7 values so AC-005.3 ownership isolation is meaningfully testable without auth
- [ ] TaskListItemDto field set confirmed as distinct from TaskDetailDto: id, title, status, dueDate (no description, no ownerId, no timestamps)
- [ ] ITaskRepository.ListAsync signature agreed: Task<(IReadOnlyList<TaskItem> Items, int Total)> ListAsync(Guid ownerId, TaskStatus? status, int page, int perPage, CancellationToken ct)
- [ ] prev/next relative-URL format agreed per api-contract.md examples: '/api/tasks?status=Pending&page=1&perPage=20' with stable canonical query-param ordering for testability
- [ ] Error shape { status, error, message, details: [{field, issue}] } confirmed as global contract per api-contract.md Section 2.3; FluentValidation validators must populate details consistently
- [ ] dueDate confirmed as DATE-ONLY (yyyy-MM-dd, nullable); createdAt/updatedAt as UTC ISO 8601 timestamps with 'Z' suffix

## Acceptance Criteria

- [ ] **AC-005.1: Returns all own tasks with no filters**
  - **Given** a user (seeded ownerId) with existing tasks across all statuses
  - **When** requesting the task list with no filters
  - **Then** the response returns HTTP 200 with all tasks belonging to that owner, mapped to TaskListItemDto

- [ ] **AC-005.2: Empty task list returns empty items, not an error**
  - **Given** a user (seeded ownerId) with zero tasks
  - **When** requesting the task list
  - **Then** the response returns HTTP 200 with items: [] and paging.total: 0, never an error

- [ ] **AC-005.3: Ownership isolation across multiple owners**
  - **Given** tasks exist for multiple distinct seeded owners
  - **When** requesting the task list as a specific owner
  - **Then** only tasks whose ownerId matches the caller's seeded ownerId are returned; paging.total reflects only own-task count

- [ ] **AC-005.4: Item field set exposed by TaskListItemDto**
  - **Given** a list response is returned
  - **When** inspecting any item in the items array
  - **Then** each item exposes id, title, status, and dueDate (dueDate may be null); no additional fields are leaked

- [ ] **AC-005.5: Default pagination when no params supplied**
  - **Given** no page or perPage query params supplied
  - **When** requesting the task list
  - **Then** the first page is returned using the server-configured DefaultPerPage, paging.page == 1, paging.prev is null

- [ ] **AC-005.6: Second page returns prev link and correct slice**
  - **Given** a result set spanning more than one page
  - **When** requesting page=2
  - **Then** paging.prev is a relative URL pointing to page 1 preserving any active filters, and items reflect the correct offset slice for page 2

- [ ] **AC-005.7: Page beyond last available page**
  - **Given** a page number beyond the last available page
  - **When** requesting that page
  - **Then** items is [], paging.total reflects the correct (non-zero) total, paging.next is null, HTTP 200

- [ ] **AC-005.8: Invalid page value returns 400**
  - **Given** page is 0, negative, or non-integer (e.g., 'abc', '1.5')
  - **When** requesting the task list
  - **Then** HTTP 400 with standard error shape and details:[{field:'page', issue}]

- [ ] **AC-005.9: perPage exceeding server maximum returns 400**
  - **Given** perPage exceeds the server maximum of 100
  - **When** requesting the task list
  - **Then** HTTP 400 with standard error shape and details:[{field:'perPage', issue:'exceeds maximum of 100'}]

- [ ] **AC-005.10: Invalid perPage value returns 400**
  - **Given** perPage is 0, negative, or non-integer
  - **When** requesting the task list
  - **Then** HTTP 400 with standard error shape and details:[{field:'perPage', issue}]

- [ ] **AC-005.11: Paging links preserve active status filter**
  - **Given** an active status filter applied together with pagination
  - **When** paging.prev or paging.next is constructed
  - **Then** the relative URL preserves the status query param alongside page/perPage in a stable canonical param order

- [ ] **AC-005.12: Deterministic ordering under timestamp collision**
  - **Given** two or more tasks share an identical createdAt timestamp (millisecond collision)
  - **When** the list is ordered and paginated across multiple requests
  - **Then** ordering is deterministic via secondary sort key (id DESC); no item is duplicated or skipped across page boundaries

## Definition of Done

- [ ] All 10 listed test cases pass plus any new boundary tests added during implementation
- [ ] GET /api/tasks implemented per Clean Architecture layering: Domain -> Application -> Infrastructure -> API
- [ ] ListTasksQueryHandler implemented against ITaskRepository with unit tests using NSubstitute mock
- [ ] TaskRepository.ListAsync implemented with EF Core LINQ only (no raw SQL), integration-tested against real PostgreSQL via Testcontainers
- [ ] ListTasksQueryValidator uses FluentValidation with CascadeMode.Continue; rejects page < 1, perPage < 1, perPage > MaxPerPage with standard error shape containing field-level details
- [ ] Response matches exact contract shape: { items, paging: { page, perPage, total, prev, next } }
- [ ] Ordering by createdAt DESC, id DESC verified by explicit test with 3+ tasks including timestamp-collision scenario
- [ ] prev/next URLs are relative, preserve status filter when present, and are null at first/last page boundaries
- [ ] Ownership filter (WHERE ownerId = @currentOwnerId) present in query even though not yet JWT-driven
- [ ] Domain layer has ZERO external package references; no EF/Npgsql types leak into Application or Domain
- [ ] Swagger/OpenAPI updated for GET /api/tasks with query params and response schema
- [ ] No InMemory/SQLite provider used anywhere in test project
- [ ] Code reviewed and merged to feature branch

## Deliverables

- `src/TaskFlow.Domain/Entities/TaskItem.cs`
- `src/TaskFlow.Domain/Enums/TaskStatus.cs`
- `src/TaskFlow.Domain/Repositories/ITaskRepository.cs`
- `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs`
- `src/TaskFlow.Application/Common/Pagination/PaginationDefaults.cs`
- `src/TaskFlow.Application/Common/Pagination/PagedResult.cs`
- `src/TaskFlow.Application/Tasks/Queries/ListTasks/ListTasksQuery.cs`
- `src/TaskFlow.Application/Tasks/Queries/ListTasks/ListTasksQueryHandler.cs`
- `src/TaskFlow.Application/Tasks/Queries/ListTasks/ListTasksQueryValidator.cs`
- `src/TaskFlow.Application/Tasks/Dtos/TaskListItemDto.cs`
- `src/TaskFlow.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs`
- `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs`
- `src/TaskFlow.Infrastructure/Auth/HardcodedCurrentUserContext.cs`
- `src/TaskFlow.Infrastructure/Persistence/Seed/TaskSeed.cs`
- `src/TaskFlow.API/Controllers/TasksController.cs`
- `src/TaskFlow.API/Common/PagingLinkBuilder.cs`
- `src/TaskFlow.API/Options/PaginationOptions.cs`
- `tests/TaskFlow.Application.Tests/Tasks/Queries/ListTasksQueryHandlerTests.cs`
- `tests/TaskFlow.Application.Tests/Tasks/Queries/ListTasksQueryValidatorTests.cs`
- `tests/TaskFlow.Application.Tests/Common/PagingLinkBuilderTests.cs`
- `tests/TaskFlow.IntegrationTests/Tasks/ListTasksEndpointTests.cs`

## Test Plan

| Test Name | AC | Assertion |
|-----------|-----|-----------|
| ListTasks_WithNoFilter_ReturnsAllOwnStatuses | AC-005.1 | response.items contains exactly the seeded owner's tasks across all statuses; count matches seeded fixture |
| ListTasks_WhenUserHasNoTasks_ReturnsEmptyItemsNotError | AC-005.2 | HTTP 200, items == [], paging.total == 0 |
| ListTasks_OnlyReturnsTasksOwnedByCaller | AC-005.3 | seed two owners with tasks; response for owner A contains zero tasks belonging to owner B; paging.total matches owner A count only |
| ListTasks_EachItem_ExposesTitleStatusAndDueDate | AC-005.4 | every item deserializes into TaskListItemDto with non-null id, title, status; dueDate key present (value may be null) |
| ListTasks_NoParams_ReturnsFirstPageWithServerDefault | AC-005.5 | paging.page == 1, paging.perPage == configured DefaultPerPage, paging.prev == null |
| ListTasks_SecondPage_ReturnsPrevLinkAndCorrectItems | AC-005.6 | paging.prev is a relative URL containing page=1; items match expected second-page slice |
| ListTasks_PageBeyondTotal_ReturnsEmptyItemsWithTotal | AC-005.7 | HTTP 200, items == [], paging.total reflects real count, paging.next == null |
| ListTasks_InvalidPage_Returns400 | AC-005.8 | page=0, page=-1, page='abc' each return 400 with details containing field == 'page' |
| ListTasks_PerPageExceeds100_Returns400 | AC-005.9 | perPage=101 returns 400 with details containing field == 'perPage' |
| ListTasks_InvalidPerPage_Returns400 | AC-005.10 | perPage=0, perPage=-5 each return 400 with details containing field == 'perPage' |
| ListTasks_WithStatusFilter_PagingLinksPreserveFilter | AC-005.11 | paging.next/prev query strings include the same status value passed in the request in canonical order |
| ListTasks_CreatedAtCollision_OrdersDeterministicallyAcrossPages | AC-005.12 | seed two tasks with identical createdAt; requesting page 1 then page 2 with perPage=1 never returns the same id twice nor omits either |
| ListTasks_DefaultOrdering_ReturnsCreatedAtDescending | AC-005.1 | 3 seeded items with distinct createdAt; result items order matches descending createdAt sequence |
| ListTasks_MultipleValidationErrors_ReturnsAllDetailsCascadeContinue | AC-005.8 | page=-1 AND perPage=500 together; error details contains entries for both 'page' and 'perPage' fields (CascadeMode.Continue verified) |
| ListTasks_PerPageAtMaxBoundary_Returns200 | AC-005.9 | perPage=100 accepted as valid boundary, returns HTTP 200 |

## Validation Rules

- page must be an integer >= 1; values <= 0, non-numeric, or decimal return 400 with field 'page'
- perPage must be an integer >= 1 and <= MaxPerPage (100); values outside range return 400 with field 'perPage'
- status must be one of 'Pending', 'In Progress', 'Completed' (exact-match, case-sensitive) or omitted; invalid values return 400 with field 'status' and the list of valid values
- FluentValidation CascadeMode.Continue: multiple invalid params in a single request return ALL field errors in a single details array, not short-circuited
- Pagination defaults (page=1, perPage=DefaultPerPage) applied when params are omitted, never silently clamped when invalid
- Ownership filter applied at the repository LINQ level (WHERE ownerId = @currentOwnerId), never in-memory post-filter

## Risks

- Server-default perPage value is not yet pinned to a literal number -- must be fixed by Engineering (recommend 20) before DOR is satisfied and before any test can assert paging.perPage == expected
- Ordering by createdAt DESC alone can produce flaky test ordering with bulk-seeded data (millisecond collisions) -- mitigated by adding id DESC as secondary sort key; seed data should use distinct createdAt values where possible
- Relative URL construction for prev/next is easy to get subtly wrong across environments (double-encoding, missing filter params) -- PagingLinkBuilder must be unit-tested in isolation, not only via integration tests
- Ownership isolation without auth (Delivery 1) risks divergence from the eventual JWT-based filter in Delivery 3 if the ICurrentUserContext seam is not cleanly implemented -- must be reviewed to confirm Delivery 3 swaps only the DI registration, not handler/test logic
- COUNT query for paging.total on every list request adds a second DB round-trip -- acceptable at seed-scale data but must be monitored once real volumes are known
- Testcontainers PostgreSQL integration tests require Docker in CI -- confirm CI pipeline provisions Docker-in-Docker or equivalent before sprint start

## Out of Scope

- JWT-based ownership resolution (Delivery 3) -- Delivery 1 uses hardcoded/seeded ownerId via ICurrentUserContext
- Sorting by fields other than createdAt DESC (no client-configurable sort order)
- Full-text search across task titles or descriptions
- Client-side (Angular) list rendering -- covered by US-009 deliverables, not this backend story
- Rate limiting or throttling on the list endpoint
- Soft-delete or archived task filtering
- Numbered/jump-to-page pagination UI (only prev/next per API contract)

## Notes

- This can be implemented as a query parameter on the list endpoint
- Only filters own tasks (ownership rule always applies)

## Related Documents

- [API Contract — List Tasks](../architecture/api-contract.md#42-list-tasks--get-apitasks) — request/response shape and error codes
- [Testing Strategy — US-005 coverage](../architecture/testing-strategy.md#us-005--list-tasks-get-apitasks)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-009 — Filter Tasks](US-009-filter-tasks-by-status.md) — filtering extension on this endpoint
