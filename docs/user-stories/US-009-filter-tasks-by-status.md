> [📚 INDEX](../INDEX.md) / [EP01 — Task Management](../epics/EP01-task-management.md) / US-009

# US-009 — Filter Tasks by Status

**Epic**: [EP01 - Task Management](../epics/EP01-task-management.md)
**Priority**: Should Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **filter my tasks by status** so that **I can focus on tasks in a specific state**.

## Definition of Ready

- [x] US-005 list endpoint is implemented and stable (this story extends it with a status filter query parameter, not a separate endpoint)
- [x] Valid status enum values confirmed as exact strings: 'Pending', 'In Progress', 'Completed' (case-sensitive exact match; lowercase 'pending' or 'PENDING' must return 400)
- [x] 400 error response for invalid status confirmed per api-contract.md Section 7: details:[{field:'status', issue:"Must be one of: 'Pending', 'In Progress', 'Completed'."}]
- [x] US-006 detail endpoint is implemented (required for ViewTaskDetail_FromList e2e test)
- [x] FluentValidation CascadeMode.Continue confirmed: invalid status AND invalid page in the same request return details entries for both fields
- [x] Confirmed this story does NOT introduce a new endpoint -- it adds a query parameter to the existing GET /api/tasks from US-005, reusing ListTasksQuery/ListTasksQueryHandler
- [ ] Frontend Angular task list page UX confirmed: filter control is a select/dropdown with options All, Pending, In Progress, Completed; 'All' maps to omitted status param
- [x] Filter state reflected in and restored from route query params for deep-link/bookmark support

## Acceptance Criteria

- [x] **AC-009.1: Filter by single status returns only matching own tasks**
  - **Given** tasks exist in Pending, In Progress, and Completed statuses for the owner
  - **When** filtering by status=Pending
  - **Then** only tasks with status exactly 'Pending' owned by the caller are returned

- [x] **AC-009.2: Filter with no matches returns empty items, not error**
  - **Given** no tasks match the requested status filter for the current owner
  - **When** requesting the filtered list
  - **Then** returns HTTP 200 with items: [] and paging.total: 0, not an error

- [x] **AC-009.3: Invalid status value returns 400**
  - **Given** an invalid status string (not one of Pending, In Progress, Completed) including case mismatches ('pending', 'PENDING'), trailing/leading whitespace ('Pending '), or unknown values ('Done')
  - **When** requesting the filtered list
  - **Then** returns HTTP 400 with details:[{field:'status', issue:"Must be one of: 'Pending', 'In Progress', 'Completed'."}]

- [x] **AC-009.4: No filter returns all statuses**
  - **Given** no status query param is supplied
  - **When** requesting the list
  - **Then** all tasks are returned regardless of status, identical behavior to US-005 AC-005.1

- [x] **AC-009.5: Filter preserved across paging navigation**
  - **Given** a status filter is active and the user navigates to a next/prev page via paging links
  - **When** following paging.next or paging.prev
  - **Then** the resulting list still reflects the active filter; paging.total reflects only the count of tasks matching the filter

- [x] **AC-009.6: Filter restored from deep-linked URL on load**
  - **Given** a user loads a URL with a status query param already set (e.g., deep link or bookmark)
  - **When** TaskListComponent initializes
  - **Then** the filter control reflects that status on load and the initial request includes the status param

## Definition of Done

- [x] ListTasksQueryValidator extended with status enum validation rule (FluentValidation, CascadeMode.Continue) returning 400 with valid enum values in details
- [x] Status filter applied at the repository LINQ level (WHERE clause) before pagination, not in-memory post-filter; paging.total reflects post-filter count
- [x] Invalid status values rejected at validation layer before reaching Application/Domain
- [ ] Frontend Angular TaskStatusFilterComponent wired to TaskListComponent; selecting a filter re-requests with status param and resets to page 1
- [x] Filter state preserved in URL query params; deep links restore filter on load
- [x] Empty state distinguishes filtered-empty ('No tasks with this status') from unfiltered-empty ('No tasks yet')
- [x] All 4 existing backend tests pass plus new filter validation tests
- [x] All 4 e2e tests pass against running full stack (API + Angular) -- not mocked
- [ ] Swagger/OpenAPI updated to document the status enum values for the query param
- [ ] Code reviewed and merged to feature branch

## Deliverables

- `src/TaskFlow.Application/Tasks/Queries/ListTasks/ListTasksQuery.cs` (Status filter property added)
- `src/TaskFlow.Application/Tasks/Queries/ListTasks/ListTasksQueryValidator.cs` (status enum validation rule with allowed-values message)
- `src/TaskFlow.Domain/Enums/TaskStatus.cs` (canonical enum definition with exact display-string mapping for 'In Progress')
- `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` (status filter applied via LINQ Where before paging)
- `src/TaskFlow.API/Controllers/TasksController.cs` (status query param binding)
- `src/frontend/src/app/features/tasks/task-list/task-list.component.ts` (filter control integration)
- `src/frontend/src/app/features/tasks/task-list/task-list.component.html`
- `src/frontend/src/app/features/tasks/task-filter/task-status-filter.component.ts`
- `src/frontend/src/app/features/tasks/task-filter/task-status-filter.component.html`
- `src/frontend/src/app/features/tasks/task-filter/task-status-filter.component.spec.ts`
- `src/frontend/src/app/features/tasks/models/task-status.enum.ts`
- `tests/TaskFlow.Application.Tests/Tasks/Queries/ListTasksStatusFilterTests.cs`
- `tests/TaskFlow.IntegrationTests/Tasks/ListTasksStatusFilterEndpointTests.cs`
- `e2e/src/tests/filter-tasks-by-status.spec.ts`
- `e2e/src/tests/view-task-detail-from-list.spec.ts`
- `e2e/src/tests/list-tasks-pagination.spec.ts`

## Test Plan

| Test Name | AC | Assertion |
|-----------|-----|-----------|
| ListTasks_FilterByValidStatus_ReturnsOnlyMatchingOwnTasks | AC-009.1 | all returned items have status == 'Pending' and belong to the caller; items of other statuses are excluded |
| ListTasks_FilterWithNoMatches_ReturnsEmptyItemsNotError | AC-009.2 | HTTP 200, items == [], paging.total == 0 |
| ListTasks_FilterWithInvalidEnumValue_Returns400ValidationError | AC-009.3 | status 400; details[0].field == 'status'; details[0].issue lists Pending, In Progress, Completed |
| ListTasks_FilterOmitted_ReturnsAllStatuses | AC-009.4 | items include tasks across all three seeded statuses |
| ListTasks_FilterWithMismatchedCase_Returns400 | AC-009.3 | 'pending' (lowercase) returns 400 with valid enum values listed |
| ListTasks_FilterWithIrregularWhitespace_Returns400 | AC-009.3 | 'In  Progress' (double space) and 'Pending ' (trailing space) both return 400 |
| ListTasks_FilterByStatus_PagingTotalReflectsFilteredCountOnly | AC-009.5 | seed 5 Pending + 3 Completed; filtering status=Pending returns paging.total == 5, not 8 |
| ListTasks_FilterByStatus_ExcludesOtherOwnersEvenWhenStatusMatches | AC-009.1 | seed same-status rows for 2 distinct owners; filtered result only contains caller-owned rows |
| ListTasks_FilterValidStatusWithInvalidPage_Returns400ForBothFields | AC-009.3 | status=Pending AND page=-1 returns 400 with details entry for 'page' (status valid, so no status error); confirms filter validity does not mask pagination validation |
| FilterTasksByStatus_FromUI_ShowsOnlyMatchingTasks | AC-009.1 | e2e: selecting 'Pending' filter in UI dropdown renders only Pending task rows |
| FilterTasksByStatus_NoMatches_ShowsEmptyState | AC-009.2 | e2e: filtering to a status with no results shows the empty-state UI component, not an error toast |
| ViewTaskDetail_FromList_ShowsFullTaskInfo | AC-009.6 | e2e: clicking a list item navigates to detail view showing all US-006 fields (description, ownerId, createdAt, updatedAt) |
| ListTasks_NavigatePages_UpdatesListAndPagingControls | AC-009.5 | e2e: clicking next/prev page controls updates rendered items and paging control state (page number, disabled states) while preserving the active filter |

## Validation Rules

- status must be one of 'Pending', 'In Progress', 'Completed' -- exact match, case-sensitive; invalid values return 400 listing the three valid options
- Case-insensitive matching is explicitly NOT supported: 'pending', 'PENDING', 'PeNdInG' all return 400
- Trailing/leading whitespace is NOT trimmed: 'Pending ' and ' Pending' return 400
- Status filter is applied via LINQ Where clause at the repository level before pagination (filter-then-paginate), never in-memory post-filter
- paging.total must reflect the count AFTER applying both status filter and ownership filter, not the unfiltered total
- FluentValidation CascadeMode.Continue: invalid status AND invalid page/perPage in the same request return details entries for all invalid fields
- Frontend filter control is a closed enum select bound to the three valid values plus 'All'; no free-text input allowed

## Risks

- 'In Progress' contains a literal space -- URL encoding (%20 vs +) must be handled consistently by both Angular HttpClient query serialization and ASP.NET Core model binding; inconsistency causes exact-match tests to flake
- Frontend filter control must use the same literal enum strings as the backend contract; any drift (e.g., UI labeling 'InProgress' vs backend 'In Progress') breaks the filter silently as a 400
- Case-sensitivity of status matching is a UX trap for API consumers typing query params manually -- must be documented prominently in Swagger/OpenAPI examples
- This story has a hard sequencing dependency on US-005 and US-006 both being complete and merged -- e2e tests will fail for unrelated reasons if sprint planning parallelizes these stories
- If status filter is applied in-memory after fetching all rows instead of via LINQ Where translated to SQL, both performance degrades and paging.total would be computed incorrectly against the unfiltered set
- e2e tests depend on stable seed data with known statuses across environments -- flaky seed ordering or missing seed data could cause false passes/fails

## Out of Scope

- Multi-value status filter (e.g., status=Pending,Completed) -- only single exact-match value per request
- Case-insensitive matching or fuzzy/partial status matching
- Filtering by other fields (dueDate range, title search) -- separate future stories
- Saved/persisted filter preferences per user (e.g., localStorage) beyond current URL query param
- New statuses beyond Pending, In Progress, Completed
- Any backend endpoint changes beyond the validator rule and LINQ filter -- this story reuses US-005's endpoint entirely

## Notes

- This can be implemented as a query parameter on the list endpoint (US-005)
- Only filters own tasks (ownership rule always applies)

## Related Documents

- [API Contract — Filtering](../architecture/api-contract.md#7-filtering) — query parameter behavior and error codes
- [Testing Strategy — US-009 coverage](../architecture/testing-strategy.md#us-009--filter-tasks-by-status-get-apitasksstatus)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-005 — List Tasks](US-005-list-tasks.md) — base endpoint this story extends
