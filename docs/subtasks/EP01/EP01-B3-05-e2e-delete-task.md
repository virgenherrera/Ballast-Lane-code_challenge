# Handoff: EP01-B3-05 — E2E: delete-task.spec.ts

## 1. Metadata

| Field        | Value                                                           |
| ------------ | --------------------------------------------------------------- |
| Task ID      | EP01-B3-05                                                      |
| Task Name    | E2E: Playwright delete-task.spec.ts                              |
| Batch        | 3 of N (EP01 Chunk-D)                                           |
| Epic         | EP01 — Task Management                                          |
| User Stories | US-008 (AC-008.1, AC-008.5 — UI layer)                          |
| Persona      | Debbie O'Brien — Playwright Team PM                              |
| Model Tier   | sonnet                                                          |

## 2. Objective

Write the Playwright E2E test `DeleteTask_FromUI_RemovesFromListAndConfirms` that exercises the full delete flow: user sees a task in the list, clicks the delete button, confirms the `window.confirm()` dialog, the task disappears from the DOM without a full page reload, and no console errors are logged. This is the single E2E test required by the DOD's test plan.

## 3. Pre-Conditions

- [ ] EP01-B3-03 STATUS: DONE — DELETE /api/tasks/{id} returns 204/404
- [ ] EP01-B3-04 STATUS: DONE — Delete button exists in task list with `data-testid="delete-btn-{id}"`, `window.confirm()` gates the call
- [ ] Full stack is running (Docker Compose or local) — `taskflow-api`, `taskflow-web`, `postgres`
- [ ] Existing E2E tests (`create-task.spec.ts`, `update-task.spec.ts`) pass

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `e2e/src/tests/tasks/create-task.spec.ts` | all | Pattern reference: how to seed a task, navigate, assert DOM |
| `e2e/src/tests/tasks/update-task.spec.ts` | all | Pattern reference: API seeding via `request.post`, wait for selector, assert DOM state |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.html` | all | DOM structure: data-testid attributes, button location |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.ts` | all | Component logic: onDelete uses window.confirm |

## 5. Deliverables

### Files to Create

| File Path | Contents |
|-----------|----------|
| `e2e/src/tests/tasks/delete-task.spec.ts` | Playwright E2E test for delete flow |

### Files to Modify

None.

### Expected Test Structure

```typescript
// delete-task.spec.ts
import { expect, test } from '@playwright/test';

test.describe('Delete Task', () => {
  test('DeleteTask_FromUI_RemovesFromListAndConfirms', async ({ page, request }) => {
    // Arrange: seed a task via direct API call
    const createResponse = await request.post('/api/tasks', {
      data: {
        title: `E2E delete test ${Date.now()}`,
        dueDate: new Date(Date.now() + 86400000).toISOString(),
      },
    });
    const task = await createResponse.json();

    // Navigate to task list and wait for task to appear
    await page.goto('/tasks');
    await page.waitForSelector(`[data-testid="delete-btn-${task.id}"]`);

    // Capture console errors
    const consoleErrors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Act: accept the confirmation dialog, then click delete
    page.on('dialog', (dialog) => dialog.accept());

    // Wait for the DELETE request to complete
    const [deleteResponse] = await Promise.all([
      page.waitForResponse(
        (res) => res.request().method() === 'DELETE' && res.url().includes(`/api/tasks/${task.id}`)
      ),
      page.click(`[data-testid="delete-btn-${task.id}"]`),
    ]);

    // Assert 1: DELETE returned 204
    expect(deleteResponse.status()).toBe(204);

    // Assert 2: task is removed from DOM
    await expect(page.getByText(task.title)).not.toBeVisible();

    // Assert 3: no full page reload occurred (URL unchanged)
    expect(page.url()).toContain('/tasks');

    // Assert 4: no console errors
    expect(consoleErrors).toEqual([]);
  });

  test('DeleteTask_CancelConfirmation_TaskRemainsInList', async ({ page, request }) => {
    // Arrange: seed a task
    const createResponse = await request.post('/api/tasks', {
      data: {
        title: `E2E cancel delete ${Date.now()}`,
        dueDate: new Date(Date.now() + 86400000).toISOString(),
      },
    });
    const task = await createResponse.json();

    await page.goto('/tasks');
    await page.waitForSelector(`[data-testid="delete-btn-${task.id}"]`);

    // Act: dismiss the confirmation dialog, then click delete
    page.on('dialog', (dialog) => dialog.dismiss());
    await page.click(`[data-testid="delete-btn-${task.id}"]`);

    // Assert: task still visible
    await expect(page.getByText(task.title)).toBeVisible();
  });
});
```

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | E2E delete test | `cd e2e && npx playwright test src/tests/tasks/delete-task.spec.ts 2>&1; echo "EXIT:$?"` | exit 0, 2 tests passed |
| G2 | Dialog automation uses native confirm | Code review: test uses `page.on('dialog', ...)` with `dialog.accept()` — confirms `window.confirm()` is the Angular implementation (not a custom modal) | verified |
| G3 | No full reload assertion | Code review: test asserts URL remains `/tasks` after delete — no `page.goto` or navigation event | verified |
| G4 | Console error capture | Code review: test captures `page.on('console')` errors and asserts empty array | verified |
| G5 | Regression — existing E2E | `cd e2e && npx playwright test 2>&1; echo "EXIT:$?"` | exit 0, all existing E2E tests pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Create new seed data or fixtures beyond the API-call seeding pattern established by existing E2E tests
- Test cross-owner deletion scenarios (covered by integration tests in B3-03)
- Test malformed GUID scenarios (cross-cutting, out of scope per story)
- Add visual regression testing or screenshot comparisons
- Modify any source code file (`.ts`, `.html`, `.cs`) — this handoff only creates the E2E test file
- Test error-state UI behavior (toast/notification on failure) — out of scope per DOD
- Test bulk delete or keyboard shortcuts

### SCOPE BOUNDARY — Stop when:

- `delete-task.spec.ts` exists with both test cases
- All E2E tests pass (including existing ones)
- Do NOT modify any application code

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| `page.locator('.delete-btn').click()` (CSS class selector) | Fragile — CSS classes change without semantic intent | `page.click('[data-testid="delete-btn-{id}"]')` — stable test ID |
| `await page.reload()` then check absence | Tests page-load behavior, not reactive state update | Assert `not.toBeVisible()` without reload — proves state-driven removal |
| Not registering dialog handler before click | `window.confirm()` blocks synchronously — Playwright needs the handler registered BEFORE the click triggers the dialog | `page.on('dialog', ...)` THEN click |
| Ignoring console errors | Empty-body JSON.parse errors would be silently swallowed | Capture via `page.on('console')` and assert empty |
| Creating test tasks via UI form | Slow, fragile, tests two features at once | Seed via `request.post('/api/tasks', ...)` — direct API call, fast |

## 9. Rollback Guidance

1. If G1 fails with dialog timeout: verify `page.on('dialog', ...)` is registered BEFORE the click — `window.confirm()` is synchronous and Playwright must be ready
2. If G1 fails with task still visible: verify the Angular component's `onDelete` method actually calls `tasks.update()` in the subscribe callback — check B3-04 was completed correctly
3. If G1 fails with console errors: likely an interceptor JSON.parse issue on empty 204 body — report to orchestrator as a B3-04 regression
4. If G5 fails: the new test file should not affect existing tests — check for port conflicts or shared state
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- E2E tests: Playwright against full stack
- ALL tests must pass before any commit
- Tests map directly to user story acceptance criteria
- E2E tests consume artifacts from BOTH api and web builds

### TASKFLOW-ANTI-DRIFT
- Seed via API calls, not UI interaction — per existing E2E pattern
- `data-testid` attributes are the stable contract between component and E2E test
- `window.confirm()` is the confirmation mechanism — Playwright automates via `page.on('dialog')`

### TASKFLOW-BUILD-PIPELINE
- E2E tests do NOT build anything themselves — they consume running services
- Same pipeline in every environment: local, CI, Docker

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B3-05
FILES_CREATED: [e2e/src/tests/tasks/delete-task.spec.ts]
FILES_MODIFIED: []
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G2 native dialog automation; confirm G3 no reload; confirm G4 console error capture}
```
