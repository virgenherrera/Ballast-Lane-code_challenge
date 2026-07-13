# Handoff: EP01-B4-03 — E2E: List Pagination, Status Filter, Detail View

## 1. Metadata

| Field        | Value                                                                    |
| ------------ | ------------------------------------------------------------------------ |
| Task ID      | EP01-B4-03                                                               |
| Task Name    | E2E: filter-tasks-by-status + view-task-detail + list-tasks-pagination   |
| Batch        | 4 of 4 (EP01 Chunk-R — Read Operations)                                 |
| Epic         | EP01 — Task Management                                                    |
| User Stories | US-005 (pagination e2e), US-006 (detail e2e), US-009 (filter e2e)        |
| Persona      | Kent C. Dodds — E2E Testing                                              |
| Model Tier   | sonnet                                                                    |

## 2. Objective

Write three Playwright E2E spec files that validate the full-stack read flows (list pagination, status filtering, detail view) against the real running application (API + Angular, not mocked). These specs prove the US-009 DOD requirement that "all 4 e2e tests pass against full stack" plus cross-story integration between list navigation and detail view. Seed test data via direct API calls, interact through the Angular UI, and assert correct rendering, URL query param sync, and API-level response correctness.

## 3. Pre-Conditions

- [ ] EP01-B4-01 STATUS: DONE — backend list and detail endpoints implemented and tested
- [ ] EP01-B4-02 STATUS: DONE — frontend components implemented, TEMPORARY stub removed
- [ ] Docker Compose stack is running (`docker compose up` exits cleanly with API + Angular + PostgreSQL)
- [ ] `cd e2e && pnpm install` exits 0
- [ ] Existing e2e tests pass: `cd e2e && pnpm exec playwright test` exits 0

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `e2e/playwright.config.ts` | all | Test runner config, baseURL, project setup |
| `e2e/src/tests/tasks/create-task.spec.ts` | all | Reference: existing task e2e pattern (API seeding, page interaction) |
| `e2e/src/tests/tasks/delete-task.spec.ts` | all | Reference: existing task e2e pattern (direct API calls for setup) |
| `e2e/src/constants/selectors.constants.ts` | all | Reference: existing selectors pattern |
| `docs/architecture/api-contract.md` | 260-345 | Sections 4.2, 4.3 — list and detail response contracts |
| `docs/architecture/api-contract.md` | 466-575 | Sections 7, 8 — filter and pagination contracts |

### Data-testid Contract (from EP01-B4-02)

The frontend components expose these `data-testid` attributes:
- `data-testid="task-list"` — list container
- `data-testid="task-list-item"` — each task row
- `data-testid="status-filter"` — filter `<select>` dropdown
- `data-testid="page-prev"` — previous page button
- `data-testid="page-next"` — next page button
- `data-testid="task-detail"` — detail view container
- `data-testid="empty-state"` — empty state message

### Seed Data Strategy

All e2e tests seed their own data via direct `POST /api/tasks` API calls in the `test.beforeEach` or test arrange phase. Do NOT rely on pre-existing database state. Each test creates exactly the tasks it needs and verifies against those specific tasks.

The API's `ICurrentUserContext` is a fixed seed shim (`SeedOwnerId`), so all tasks created via the API belong to the same owner — cross-owner isolation is tested at the integration level (EP01-B4-01), not e2e.

## 5. Deliverables

### Files to Create

| File Path | Contents |
|-----------|----------|
| `e2e/src/tests/tasks/filter-tasks-by-status.spec.ts` | 2 e2e tests: filter shows only matching tasks, no-matches shows empty state |
| `e2e/src/tests/tasks/view-task-detail-from-list.spec.ts` | 1 e2e test: click task in list, navigate to detail, verify all 8 fields |
| `e2e/src/tests/tasks/list-tasks-pagination.spec.ts` | 1 e2e test: seed enough tasks for 2+ pages, navigate pages, verify list updates and paging controls |

### Files to Modify

None. E2E specs are additive only.

### Required Test Names

```typescript
// filter-tasks-by-status.spec.ts
test.describe('Filter Tasks by Status', () => {
  test('FilterTasksByStatus_FromUI_ShowsOnlyMatchingTasks', ...)
  // Arrange: create 3 tasks via API — one Pending, one In Progress (via PATCH),
  //   one Completed (via PATCH)
  // Act: navigate to /tasks, select "In Progress" from status filter dropdown
  // Assert: only the In Progress task is visible; other tasks are NOT visible;
  //   URL contains ?status=In%20Progress or ?status=In+Progress

  test('FilterTasksByStatus_NoMatches_ShowsEmptyState', ...)
  // Arrange: create 1 task (defaults to Pending)
  // Act: navigate to /tasks, select "Completed" from filter
  // Assert: empty-state element visible; no task-list-item elements;
  //   task-list container still present (not an error state)
});
```

```typescript
// view-task-detail-from-list.spec.ts
test.describe('View Task Detail', () => {
  test('ViewTaskDetail_FromList_ShowsFullTaskInfo', ...)
  // Arrange: create 1 task via API with title + description + dueDate
  // Act: navigate to /tasks, click the task row/link to navigate to detail
  // Assert: URL is /tasks/{id}; detail container visible; all 8 fields
  //   rendered (title, description, status, dueDate, ownerId, createdAt,
  //   updatedAt, id)
});
```

```typescript
// list-tasks-pagination.spec.ts
test.describe('List Tasks Pagination', () => {
  test('ListTasks_NavigatePages_UpdatesListAndPagingControls', ...)
  // Arrange: create 25 tasks via API (> DefaultPerPage of 20)
  // Act: navigate to /tasks
  // Assert page 1: task-list-item count == 20; page-prev is disabled;
  //   page-next is enabled
  // Act: click page-next
  // Assert page 2: task-list-item count == 5; page-next is disabled;
  //   page-prev is enabled; URL contains ?page=2
  // Act: click page-prev
  // Assert page 1 again: task-list-item count == 20; page-prev disabled
});
```

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Filter e2e | `cd e2e && pnpm exec playwright test src/tests/tasks/filter-tasks-by-status.spec.ts` | exit 0, 2 tests passed |
| G2 | Detail e2e | `cd e2e && pnpm exec playwright test src/tests/tasks/view-task-detail-from-list.spec.ts` | exit 0, 1 test passed |
| G3 | Pagination e2e | `cd e2e && pnpm exec playwright test src/tests/tasks/list-tasks-pagination.spec.ts` | exit 0, 1 test passed |
| G4 | Regression | `cd e2e && pnpm exec playwright test` | exit 0, all existing + new tests pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Write tests for functionality not implemented (no search tests, no multi-filter tests, no sort tests)
- Mock HTTP responses — all tests run against the real full stack
- Modify any backend or frontend source code
- Add Playwright page objects unless the existing codebase already uses them for tasks
- Test cross-owner isolation at the e2e level — that is integration test scope (EP01-B4-01)
- Add more than the 4 specified test cases — this is the complete and closed set
- Test malformed GUID 400 responses at the e2e level — that is integration test scope
- Add visual regression/screenshot comparison tests

### SCOPE BOUNDARY — Stop when:

- All 4 e2e tests pass against the full running stack
- All existing e2e tests still pass (regression gate)
- Do NOT proceed to any other work

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| Hardcoding task IDs or assuming specific seed data | Database is shared/mutable; tests become flaky | Seed via API in each test, capture returned IDs, assert against those specific tasks |
| Asserting `page 2 has task X` by title | Order may vary; seed creates tasks rapidly with near-identical timestamps | Assert structurally: item count, no duplicate IDs across pages, `paging.total` matches seed count |
| Using `page.waitForTimeout(1000)` for API responses | Flaky timing; may pass locally, fail in CI | Use `page.waitForResponse` or `expect(locator).toBeVisible()` — Playwright auto-waits |
| Selecting filter by visible text without verifying URL update | Half the AC — URL must reflect filter state for deep-link support | Assert both: visible filtering AND URL query param presence |
| Relying on `task-list-stub` selector | TEMPORARY component deleted in EP01-B4-02 | Use `task-list` data-testid (new component) |
| Writing speculative placeholder tests for future features | Over-engineers; tests for unimplemented functionality always fail or are no-ops | Only the 4 named tests — nothing more |

## 9. Rollback Guidance

1. If G1 fails: verify the status filter `<select>` has `data-testid="status-filter"` and the dropdown option values match `"Pending"`, `"In Progress"`, `"Completed"` exactly
2. If G1 fails on "In Progress" filter: check URL encoding — Angular may encode as `+` instead of `%20`; assert with a regex that accepts either: `status=In(%20|\\+)Progress`
3. If G2 fails: verify task list items are clickable links/buttons that navigate to `/tasks/{id}`; check `data-testid="task-detail"` exists on the detail page
4. If G3 fails on task count: verify `DefaultPerPage` is 20 (seed 25 tasks to guarantee 2 pages); if the backend changed this value, adjust seed count to `DefaultPerPage + 5`
5. If G3 fails on pagination button state: verify `data-testid="page-prev"` and `data-testid="page-next"` exist and are `disabled` (HTML attribute) not just visually grayed
6. If G4 fails on existing e2e tests: check if `create-task.spec.ts` still references `task-list-stub` selector — it may need the `task-list` selector instead (this is a known existing-test dependency on the TEMPORARY component; if so, update the selector to `task-list` without changing test logic)
7. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- E2E tests run against the full stack (API + Angular), not mocked
- Tests map directly to user story acceptance criteria
- PostgreSQL via Docker Compose (postgres:17-alpine)

### TASKFLOW-ANTI-DRIFT
- Every decision must trace back to a requirement or AC
- Status string literals must match exactly: `'Pending'`, `'In Progress'`, `'Completed'`
- E2E tests use `data-testid` attributes for element selection — never CSS classes or tag names
- Only the 4 named test cases are in scope — no speculative tests for unimplemented features

### TASKFLOW-BUILD-PIPELINE
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web
- Docker credential workaround: scratch DOCKER_CONFIG with credsStore omitted
- E2E runner: Playwright via pnpm in the `e2e/` directory

### TASKFLOW-E2E-SEED
- Seed test data via direct `POST /api/tasks` API calls (and `PATCH` for status changes), not via database fixtures
- Each test seeds its own data and asserts against it — no shared/pre-existing state assumptions
- Use `Date.now()` in task titles for uniqueness across parallel test runs
- Clean up is implicit — Testcontainers/Docker Compose handles database lifecycle

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B4-03
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G1 filter tests pass; confirm G2 detail navigation works; confirm G3 pagination prev/next cycle works; confirm G4 no regression on existing e2e}
```
