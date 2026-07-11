# Handoff: EP01-B4-02 — Frontend: Task List, Status Filter, Detail View

## 1. Metadata

| Field        | Value                                                                      |
| ------------ | -------------------------------------------------------------------------- |
| Task ID      | EP01-B4-02                                                                 |
| Task Name    | Frontend: TaskListComponent + TaskStatusFilterComponent + TaskDetailComponent |
| Batch        | 4 of 4 (EP01 Chunk-R — Read Operations)                                   |
| Epic         | EP01 — Task Management                                                      |
| User Stories | US-005 (frontend consumption), US-006 (detail view), US-009 (AC-009.1–009.6) |
| Persona      | React/Angular Component Architect                                           |
| Model Tier   | sonnet                                                                      |

## 2. Objective

Replace the TEMPORARY `TaskListStubComponent` and TEMPORARY `TaskService.getTasks()` with production components: a `TaskListComponent` with pagination controls and status filter dropdown, a `TaskStatusFilterComponent` (standalone), and a `TaskDetailComponent` for the full task view. Update `TaskService` to consume the new paginated list contract (`{items, paging}`) and add `getTaskById`. Wire routing for `/tasks` (list) and `/tasks/:id` (detail). Sync filter and page state bidirectionally with URL query parameters so deep links restore component state on init.

## 3. Pre-Conditions

- [ ] EP01-B4-01 STATUS: DONE — backend list and detail endpoints deployed and tested
- [ ] `npx ng build` exits 0
- [ ] `npx ng test --watch=false` exits 0 (existing tests pass)
- [ ] `web/src/app/features/tasks/task-list-stub/task-list-stub.component.ts` exists (TEMPORARY, to be replaced)
- [ ] `web/src/app/features/tasks/data-access/task.service.ts` exists with TEMPORARY `getTasks()`

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `web/src/app/features/tasks/data-access/task.service.ts` | all | Replace TEMPORARY `getTasks()`, add `getTaskById` |
| `web/src/app/features/tasks/data-access/task.service.spec.ts` | all | Extend with new method tests |
| `web/src/app/features/tasks/models/task.model.ts` | all | Add `TaskListItem`, `Paging`, `TaskListResponse` types |
| `web/src/app/features/tasks/models/task.constants.ts` | all | Reference: existing constants pattern |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.ts` | all | TEMPORARY to delete |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.html` | all | TEMPORARY to delete |
| `web/src/app/features/tasks/create-task/create-task.component.ts` | all | Reference: component pattern (standalone, signals) |
| `web/src/app/features/tasks/create-task/create-task.component.spec.ts` | all | Reference: test pattern (TestBed, Angular) |
| `web/src/app/app.routes.ts` | all | Modify routing: replace stub route, add detail route |
| `web/src/environments/environment.ts` | all | API base URL |
| `docs/architecture/api-contract.md` | 260-345 | Sections 4.2, 4.3 — list and detail response shapes |
| `docs/architecture/api-contract.md` | 466-575 | Sections 7, 8 — filter and pagination contracts |

## 5. Deliverables

### Files to Create

| File Path | Contents |
|-----------|----------|
| `web/src/app/features/tasks/models/task-status.enum.ts` | Union type: `type TaskStatusFilter = 'Pending' \| 'In Progress' \| 'Completed'`; constant array `TASK_STATUSES` for dropdown options |
| `web/src/app/features/tasks/task-list/task-list.component.ts` | Standalone component: paginated list, injects `TaskService` + `ActivatedRoute` + `Router`; reads `status`/`page` from query params on init; re-fetches on param change |
| `web/src/app/features/tasks/task-list/task-list.component.html` | Template: table/list of tasks (title, status badge, dueDate), pagination prev/next buttons (disabled at boundaries), empty state message, clickable rows linking to detail |
| `web/src/app/features/tasks/task-list/task-list.component.spec.ts` | Component specs: renders list, empty state, pagination controls, filter change resets page, detail navigation |
| `web/src/app/features/tasks/task-status-filter/task-status-filter.component.ts` | Standalone component: `<select>` dropdown with All/Pending/In Progress/Completed; emits `statusChange` output; receives current status as input |
| `web/src/app/features/tasks/task-status-filter/task-status-filter.component.html` | Template: select element with `data-testid="status-filter"` |
| `web/src/app/features/tasks/task-status-filter/task-status-filter.component.spec.ts` | Component spec: dropdown renders all options, emits on change |
| `web/src/app/features/tasks/task-detail/task-detail.component.ts` | Standalone component: fetches task by ID from route param, displays all 8 fields, handles 404 with error message |
| `web/src/app/features/tasks/task-detail/task-detail.component.html` | Template: full task detail view with all 8 fields, back-to-list link |
| `web/src/app/features/tasks/task-detail/task-detail.component.spec.ts` | Component spec: renders all fields, handles null description/dueDate, shows error on 404 |

### Files to Modify

| File Path | Change |
|-----------|--------|
| `web/src/app/features/tasks/models/task.model.ts` | Add `TaskListItem` (4 fields: id, title, status, dueDate), `Paging` (page, perPage, total, prev, next), `TaskListResponse` (`{items: TaskListItem[], paging: Paging}`) interfaces |
| `web/src/app/features/tasks/data-access/task.service.ts` | Replace TEMPORARY `getTasks()` with `getTasks(params?: {status?, page?, perPage?}): Observable<TaskListResponse>`; add `getTaskById(id: string): Observable<TaskResponse>` |
| `web/src/app/features/tasks/data-access/task.service.spec.ts` | Add tests for `getTasks` with params (query string verification), `getTaskById` |
| `web/src/app/app.routes.ts` | Replace TEMPORARY stub route with `TaskListComponent`; add `tasks/:id` route for `TaskDetailComponent` |

### Files to Delete

| File Path | Reason |
|-----------|--------|
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.ts` | TEMPORARY — superseded by `TaskListComponent` |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.html` | TEMPORARY — superseded by `TaskListComponent` |

### Expected Signatures

```typescript
// task-status.enum.ts
export type TaskStatusFilter = 'Pending' | 'In Progress' | 'Completed';
export const TASK_STATUSES: readonly TaskStatusFilter[] = [
  'Pending', 'In Progress', 'Completed',
] as const;
```

```typescript
// task.model.ts — new types (add to existing file)
export interface TaskListItem {
  id: string;
  title: string;
  status: string;
  dueDate: string | null;
}

export interface Paging {
  page: number;
  perPage: number;
  total: number;
  prev: string | null;
  next: string | null;
}

export interface TaskListResponse {
  items: TaskListItem[];
  paging: Paging;
}
```

```typescript
// task.service.ts — updated methods
getTasks(params?: {
  status?: string;
  page?: number;
  perPage?: number;
}): Observable<TaskListResponse>

getTaskById(id: string): Observable<TaskResponse>
```

### FROZEN DECISIONS

**1. URL query params are the single source of truth** for filter state and page number. The component reads from `ActivatedRoute.queryParams` on init and updates via `Router.navigate` with `queryParamsHandling: 'merge'`. State flows: URL -> component -> API call. Never: component -> URL (one-directional would break deep links per AC-009.6).

**2. Filter change resets to page 1**: When the user changes the status filter, navigate to `?status=X&page=1` (or omit page param, defaulting to 1). Do NOT preserve the current page number when filters change.

**3. "All" filter maps to omitted param**: The "All" dropdown option navigates without a `status` query param. Do NOT send `status=` or `status=All` to the API.

**4. `data-testid` attributes** for e2e targeting (required by EP01-B4-03):
- `data-testid="task-list"` on the list container
- `data-testid="task-list-item"` on each task row
- `data-testid="status-filter"` on the filter `<select>`
- `data-testid="page-prev"` and `data-testid="page-next"` on pagination buttons
- `data-testid="task-detail"` on the detail container
- `data-testid="empty-state"` on the empty-state message element

**5. HttpParams encoding**: Angular's `HttpClient` with `HttpParams` encodes spaces as `+` by default. Verify this works with ASP.NET Core model binding for `"In Progress"`. If ASP.NET rejects `+`, use a custom `HttpParameterCodec` that uses `encodeURIComponent` (produces `%20`). Test this explicitly.

**6. Pagination UI**: Prev/next buttons ONLY (no numbered pages, no jump-to-page). Buttons disabled (not hidden) at boundaries (`prev` disabled on page 1, `next` disabled on last page).

**7. Empty state distinction**: When `items` is empty and no filter is active, show "No tasks yet." When `items` is empty and a filter is active, show "No tasks match the selected filter." (AC-009.2 distinction).

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Build | `cd web && npx ng build` | exit 0 |
| G2 | TaskService specs | `cd web && npx ng test --watch=false --include='**/task.service.spec.ts'` | exit 0, 6+ tests passed |
| G3 | TaskListComponent specs | `cd web && npx ng test --watch=false --include='**/task-list/task-list.component.spec.ts'` | exit 0, 5+ tests passed |
| G4 | TaskStatusFilterComponent specs | `cd web && npx ng test --watch=false --include='**/task-status-filter/task-status-filter.component.spec.ts'` | exit 0, 2+ tests passed |
| G5 | TaskDetailComponent specs | `cd web && npx ng test --watch=false --include='**/task-detail/task-detail.component.spec.ts'` | exit 0, 3+ tests passed |
| G6 | TEMPORARY removed | `grep -rn "task-list-stub\|TaskListStubComponent" web/src/` | 0 matches |
| G7 | No `any` types | `grep -rn ": any\b" web/src/app/features/tasks/task-list/ web/src/app/features/tasks/task-detail/ web/src/app/features/tasks/task-status-filter/ web/src/app/features/tasks/data-access/task.service.ts` | 0 matches |
| G8 | data-testid present | `grep -rn "data-testid" web/src/app/features/tasks/task-list/task-list.component.html web/src/app/features/tasks/task-status-filter/task-status-filter.component.html web/src/app/features/tasks/task-detail/task-detail.component.html` | At least 6 matches (task-list, task-list-item, status-filter, page-prev, page-next, task-detail) |
| G9 | Regression — all FE tests | `cd web && npx ng test --watch=false` | exit 0, all existing tests pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Add a text search input or title filter — no `<input type="text">` for searching
- Add a second filter dropdown (e.g., by date, by priority) — only status filter exists in this batch
- Implement numbered pagination, jump-to-page, or infinite scroll — prev/next only
- Add sorting controls or column-header sorting
- Persist filter preferences to localStorage or a backend store
- Add inline task editing from the list view
- Modify the `CreateTaskComponent` or its routing
- Add Angular Material, PrimeNG, or any UI library — use plain HTML + existing project CSS conventions
- Add animations or transitions
- Implement error retry/exponential backoff on API failures

### SCOPE BOUNDARY — Stop when:

- `TaskListComponent` renders paginated task list with status filter
- `TaskDetailComponent` renders full task detail from `/tasks/:id` route
- URL query params sync bidirectionally with component state
- All component specs pass
- TEMPORARY stub component and route are fully removed
- Do NOT proceed to e2e tests (that is EP01-B4-03)

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| Component state as source of truth for filter/page | Deep links (AC-009.6) break — URL and component diverge | URL query params are the single source of truth; component derives state from `ActivatedRoute.queryParams` |
| `getTasks()` returning `TaskResponse[]` (old signature) | New API returns `{items, paging}` — type mismatch | Return `Observable<TaskListResponse>` with properly typed `items` and `paging` |
| Sending `status=All` to the API | API does not recognize "All" as a valid enum value — returns 400 | Omit the `status` param entirely when "All" is selected |
| Using `HttpParams.set('status', value)` without encoding check | `"In Progress"` has a space — must verify Angular's default `+` encoding works with ASP.NET or use `%20` | Test round-trip explicitly; use custom codec if `+` fails |
| Hardcoding status strings in templates | Drift risk vs backend contract | Import from `task-status.enum.ts` constant array |
| Keeping `TaskListStubComponent` alongside new component | Two list components = routing ambiguity, dead code | Delete the entire `task-list-stub/` directory |
| Using `subscribe()` in component constructor | Race conditions with Angular lifecycle | Use `ActivatedRoute.queryParams` subscription in constructor or `ngOnInit`; unsubscribe via `takeUntilDestroyed` |

## 9. Rollback Guidance

1. If G1 fails: check import paths — deleting `task-list-stub` may break imports in `app.routes.ts` or other files that referenced it
2. If G2 fails: verify `HttpTestingController` mock matches the new URL pattern with query params (`?page=1&perPage=20` etc.)
3. If G3-G5 fail: check that `ActivatedRoute` is properly stubbed with `queryParams` observable in test setup
4. If G6 fails: search for any remaining references to `TaskListStubComponent` or `task-list-stub` selector in templates, routes, or specs
5. If G7 fails: add proper types for API response — do not use `any` as a shortcut
6. If G9 fails: check if existing `create-task` or `task.service` specs depend on the old `getTasks()` signature and update them
7. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Tests map directly to user story acceptance criteria
- Frontend tests: `npx ng test --watch=false` (Vitest via Angular CLI)

### TASKFLOW-ANTI-DRIFT
- Every decision must trace back to a requirement or AC
- Status string literals must match backend exactly: `'Pending'`, `'In Progress'`, `'Completed'`
- Single source of truth for status values: `task-status.enum.ts`
- `data-testid` attributes are required for e2e targeting — do not use CSS classes for test selection

### TASKFLOW-FRONTEND
- Angular 22.0.6, standalone components, signals for reactive state
- No `any` types on API response shapes
- Component specs use Angular TestBed with `provideHttpClient()` + `provideHttpClientTesting()`
- Route params and query params via `ActivatedRoute` injection

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B4-02
FILES_CREATED: [list]
FILES_MODIFIED: [list]
FILES_DELETED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G6 TEMPORARY removed; confirm G7 no any types; confirm G8 data-testid present; confirm deep-link restoration works per AC-009.6}
```
