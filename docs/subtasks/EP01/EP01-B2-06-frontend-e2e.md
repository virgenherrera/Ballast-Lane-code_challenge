# Handoff: EP01-B2-06 — Frontend: TaskService.updateTask() + Minimal Status Dropdown + E2E Test

## 1. Metadata

| Field        | Value                                                                        |
| ------------ | ---------------------------------------------------------------------------- |
| Task ID      | EP01-B2-06                                                                   |
| Task Name    | Frontend: TaskService.updateTask(), minimal status dropdown, E2E update-task.spec.ts |
| Batch        | 2 of N (EP01 Chunk-U)                                                        |
| Epic         | EP01 — Task Management                                                       |
| User Stories | US-007 (AC-007.2 — status change via UI; scope explicitly limited, see Section 7) |
| Persona      | Frontend Engineer + QA Automation (combined minimal-surface task)             |
| Model Tier   | sonnet                                                                       |

## 2. Objective

**Scope contradiction resolved**: US-007's Out-of-Scope section defers "FE edit dialog/form" to US-017, yet the DOD requires E2E test `UpdateTask_ChangeStatusFromUI_ReflectsInListImmediately`. Resolution: this task builds the MINIMUM FE surface to satisfy that one E2E test — a `TaskService.updateTask()` data-access method plus an inline status `<select>` dropdown in the task list. This is NOT a full edit form. Title/description/dueDate editing UI remains deferred to US-017/US-018.

**Verified fact**: The Angular frontend (`web/`) currently contains only a health component. No `TaskService`, no task list view, and no task-related components exist. The Chunk-C frontend tasks (EP01-B1-05a/05b) have NOT been applied to the working tree. Therefore, this task's pre-conditions will likely BLOCK until Chunk-C's frontend work is delivered.

**E2E**: `e2e/` directory uses Playwright (TypeScript, not the .NET Playwright). Only `health-semaphore.spec.ts` exists today.

## 3. Pre-Conditions

- [ ] EP01-B2-05 STATUS: DONE — all 14 integration tests pass (backend is fully working)
- [ ] `dotnet test` (full solution) exits 0 — backend regression-free
- [ ] An Angular `TaskService` (or equivalent) exists in `web/src/app/` with at least a `createTask()` method — needed as pattern reference for `updateTask()`
- [ ] A task-list view/component exists in `web/src/app/` displaying tasks fetched from the API — needed to wire the status dropdown into
- [ ] `e2e/playwright.config.ts` exists and can target the running Angular + API stack

**If the TaskService or task-list component does NOT exist** (i.e., Chunk-C frontend was not applied), report BLOCKED with the exact missing file paths. Do NOT build the entire task list/service from scratch under this task.

## 4. Context Bundle

*(Read only if pre-conditions are confirmed satisfied at execution time.)*

| File | Lines | Why |
|------|-------|-----|
| Existing `TaskService` file (path at execution time) | full | HTTP client pattern, existing methods to mirror |
| Existing task-list component (path at execution time) | full | Where to add the status dropdown |
| `e2e/playwright.config.ts` | full | Confirm test target URLs and config |
| `e2e/src/tests/health-semaphore.spec.ts` | full | Existing E2E test pattern (imports, fixtures, assertions) |
| `docs/architecture/api-contract.md` | 347-391 (section 4.4, corrected by B2-04) | Authoritative PATCH contract for schema |
| `docs/user-stories/US-007-update-task.md` | 122 | E2E test name |
| `docs/user-stories/US-007-update-task.md` | 146-159 | Out of Scope — confirms full edit form is deferred |

## 5. Deliverables

### Files to Modify

| File Path | Change |
|-----------|--------|
| Existing `TaskService` (path confirmed at execution) | ADD `updateTask(id: string, payload: Partial<UpdateTaskPayload>)` calling PATCH /api/tasks/{id}. On success, update local state/signal with response body (optimistic update from response — no blind refetch). On error, parse via shared error schema. |
| Existing task-list component (path confirmed at execution) | ADD a native `<select>` element per task row with `data-testid="status-select-{task.id}"`, options: Pending, In Progress, Completed. On `(change)`, call `TaskService.updateTask(task.id, { status: newValue })`. |

### Files to Create

| File Path | Contents |
|-----------|----------|
| `e2e/src/tests/update-task.spec.ts` | Single Playwright spec: `UpdateTask_ChangeStatusFromUI_ReflectsInListImmediately` |

### Expected Signatures

```typescript
// TaskService — ADD method (mirror existing createTask pattern)
updateTask(id: string, payload: { title?: string; description?: string; status?: string; dueDate?: string }): Observable<Task> {
  return this.http.patch<Task>(`${this.apiUrl}/tasks/${id}`, payload);
}
// If the service uses signals/store: after successful response, update the
// corresponding task in the local tasks array/signal with the response body.
```

```html
<!-- Task list component template — ADD per row -->
<select
  [attr.data-testid]="'status-select-' + task.id"
  [value]="task.status"
  (change)="onStatusChange(task.id, $event)">
  <option value="Pending">Pending</option>
  <option value="In Progress">In Progress</option>
  <option value="Completed">Completed</option>
</select>
```

```typescript
// Task list component — ADD handler
onStatusChange(taskId: string, event: Event): void {
  const select = event.target as HTMLSelectElement;
  this.taskService.updateTask(taskId, { status: select.value }).subscribe();
}
```

```typescript
// e2e/src/tests/update-task.spec.ts
import { test, expect } from '@playwright/test';

test('UpdateTask_ChangeStatusFromUI_ReflectsInListImmediately', async ({ page, request }) => {
  // Arrange: seed a task via direct API call (isolate test surface to update flow)
  const createResponse = await request.post('/api/tasks', {
    data: { title: 'E2E status change test', dueDate: new Date(Date.now() + 86400000).toISOString() }
  });
  const task = await createResponse.json();

  await page.goto('/tasks'); // or wherever the list lives
  await page.waitForSelector(`[data-testid="status-select-${task.id}"]`);

  // Act: change status via dropdown
  const [patchRequest] = await Promise.all([
    page.waitForRequest(req =>
      req.method() === 'PATCH' && req.url().includes(`/api/tasks/${task.id}`)
    ),
    page.selectOption(`[data-testid="status-select-${task.id}"]`, 'Completed'),
  ]);

  // Assert 1: Network — PATCH fired with correct partial body
  const body = JSON.parse(patchRequest.postData() ?? '{}');
  expect(body).toEqual({ status: 'Completed' });

  // Assert 2: DOM — list reflects new status without manual reload
  const selectValue = await page.locator(`[data-testid="status-select-${task.id}"]`).inputValue();
  expect(selectValue).toBe('Completed');
});
```

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Frontend build/lint | `cd web && npm run build` (or project's build command) | exit 0 |
| G2 | Backend regression | `dotnet test` (full solution) | exit 0 — run BEFORE E2E |
| G3 | E2E spec | `cd e2e && npx playwright test src/tests/update-task.spec.ts` | exit 0, 1 passed |
| G4 | Full E2E regression | `cd e2e && npx playwright test` | exit 0, zero new failures (health spec still passes) |
| G5 | Minimal scope verified | Code review: no multi-field edit form/dialog component created | confirmed |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Build a full multi-field edit dialog/form (title/description/dueDate editing UI) — deferred to US-017/US-018 per story Out-of-Scope
- Create a dedicated UpdateTask Zod schema for full client-side validation — keep it minimal for status-only; full schemas come with US-017
- Implement optimistic-concurrency conflict handling (ETag/If-Match)
- Seed E2E test data through the UI creation flow — use direct API call for test isolation
- Add error-toast/snackbar UI for failed updates — minimal scope, console.error is acceptable for now
- Build the TaskService or task-list from scratch if they don't exist — report BLOCKED instead

### SCOPE BOUNDARY — Stop when:

- `TaskService.updateTask()` method exists and calls PATCH correctly
- Minimal status dropdown is wired into the existing list view per row
- E2E test passes with both assertions (network + DOM)
- Full backend + full E2E regression green
- Do NOT proceed to building the full edit form

### Explicit Scope Decision (documented for the record):

E2E coverage for Chunk-U is intentionally limited to the status-change happy path (AC-007.2). All other ACs (007.1, 007.3-008) are covered at Integration level (EP01-B2-05). This is a deliberate test-pyramid decision, not an oversight.

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| Building a full edit-form dialog | Contradicts story Out-of-Scope, creeps into US-017 | Minimal inline `<select>` only |
| Refetching entire task list after PATCH | Works but doesn't satisfy "reflects immediately" tightly | Apply response body to local state directly |
| Single combined E2E assertion (no split) | Failure doesn't reveal if bug is network or rendering | Two assertions: intercept PATCH body + DOM state |
| Seeding via UI creation flow in E2E | Couples test to Create flow bugs; slower, less isolated | Direct API `request.post` for seed |
| Building TaskService/list from scratch | Scope explosion; this task depends on Chunk-C FE existing | Report BLOCKED if absent |

## 9. Rollback Guidance

1. If pre-conditions fail (no TaskService or list component): report BLOCKED with exact missing paths
2. If G3 fails: check `data-testid` selectors match what the template renders; check API proxy config routes PATCH correctly
3. If G2 fails (backend regression): STOP — do not attempt E2E against a broken backend
4. If the task-list uses a different rendering approach (e.g. virtual scroll, CDK table): adapt the `<select>` placement to the existing row template pattern
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- E2E: Playwright, final quality gate
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT
- Respect the current phase — this is the terminal gate; do not begin US-017 work
- Every decision must trace back to a requirement or AC
- Version pinning: ALL dependencies use exact versions

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17.5, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B2-06
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm minimal-scope boundary was respected (no full edit form); confirm which Chunk-C FE files were modified; if BLOCKED, list exactly which FE prerequisites were absent}
```
