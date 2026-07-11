# Handoff: EP01-B3-04 — Frontend: TaskService.deleteTask + Delete Button with Confirmation + Unit Tests

## 1. Metadata

| Field        | Value                                                           |
| ------------ | --------------------------------------------------------------- |
| Task ID      | EP01-B3-04                                                      |
| Task Name    | Frontend: deleteTask service method + delete button + confirmation dialog + unit tests |
| Batch        | 3 of N (EP01 Chunk-D)                                           |
| Epic         | EP01 — Task Management                                          |
| User Stories | US-008 (AC-008.1, AC-008.5)                                     |
| Persona      | Sarah Drasner — VP of DX / Frontend                              |
| Model Tier   | sonnet                                                          |

## 2. Objective

Add `deleteTask(id: string): Observable<void>` to `TaskService` using `HttpClient.delete<void>()`. Add a delete button per task row in the task list stub component with `data-testid="delete-btn-{task.id}"`. Wire the button to a `window.confirm()` confirmation dialog (functional only, no custom modal). On 204 success, remove the task from local signal state via array filter (no full `getTasks()` refetch, no page reload). Write unit tests covering the service method and component behavior.

**CRITICAL**: Never remove the task from list state before the 204 response arrives — no optimistic deletion. Confirmation-first, then service call, then state update on success only.

## 3. Pre-Conditions

- [ ] EP01-B3-03 STATUS: DONE — DELETE /api/tasks/{id} endpoint is live and returns 204/404
- [ ] `web/src/app/features/tasks/data-access/task.service.ts` exists with `createTask`, `getTasks`, `updateTask`
- [ ] `web/src/app/features/tasks/task-list-stub/task-list-stub.component.ts` exists with `tasks` signal and `onStatusChange`
- [ ] `web/src/app/features/tasks/task-list-stub/task-list-stub.component.html` exists with `@for` loop
- [ ] `npx ng build` exits 0 (or equivalent Angular build command)

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `web/src/app/features/tasks/data-access/task.service.ts` | all | Add `deleteTask` method here |
| `web/src/app/features/tasks/data-access/task.service.spec.ts` | all | Existing test pattern — add deleteTask tests |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.ts` | all | Add `onDelete` method here |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.html` | all | Add delete button per row here |
| `web/src/app/features/tasks/models/task.model.ts` | all | Task interface and types |
| `web/src/environments/environment.ts` | all | `apiBaseUrl` for service URL construction |

## 5. Deliverables

### Files to Create

None.

### Files to Modify

| File Path | Change |
|-----------|--------|
| `web/src/app/features/tasks/data-access/task.service.ts` | Add `deleteTask(id: string): Observable<void>` method |
| `web/src/app/features/tasks/data-access/task.service.spec.ts` | Add 2 unit tests for `deleteTask` |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.ts` | Add `onDelete(taskId: string)` method with `window.confirm` + service call + state update |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.html` | Add delete button per row with `data-testid` |

### Expected Signatures

```typescript
// In task.service.ts — add after updateTask:
deleteTask(id: string): Observable<void> {
  return this.http.delete<void>(`${environment.apiBaseUrl}/api/tasks/${id}`);
}
// Angular HttpClient.delete<void>() handles empty 204 bodies natively.
// Do NOT add responseType: 'text' unless an interceptor failure is observed.
```

```typescript
// In task-list-stub.component.ts — add method:
onDelete(taskId: string): void {
  if (!confirm('Are you sure you want to delete this task?')) {
    return;
  }

  this.taskService.deleteTask(taskId).subscribe({
    next: () => {
      this.tasks.update((tasks) => tasks.filter((t) => t.id !== taskId));
    },
    error: () => {
      // On failure, task remains in list — no state change.
      // Error handling can be enhanced in future stories.
    },
  });
}
```

```html
<!-- In task-list-stub.component.html — inside the @for loop, after the select: -->
<button
  type="button"
  [attr.data-testid]="'delete-btn-' + task.id"
  (click)="onDelete(task.id)">
  Delete
</button>
```

### Required Test Names (in task.service.spec.ts — add to existing describe block)

```typescript
it('TaskService_deleteTask_SendsDeleteToCorrectEndpoint', () => {
  const taskId = '01961234-89ab-7cde-f012-3456789abcde';
  service.deleteTask(taskId).subscribe();

  const req = httpMock.expectOne(
    `${environment.apiBaseUrl}/api/tasks/${taskId}`
  );
  expect(req.request.method).toBe('DELETE');
  req.flush(null, { status: 204, statusText: 'No Content' });
});

it('TaskService_deleteTask_CompletesWithoutErrorOn204', () => {
  const taskId = '01961234-89ab-7cde-f012-3456789abcde';
  let completed = false;
  let errored = false;

  service.deleteTask(taskId).subscribe({
    complete: () => { completed = true; },
    error: () => { errored = true; },
  });

  const req = httpMock.expectOne(
    `${environment.apiBaseUrl}/api/tasks/${taskId}`
  );
  req.flush(null, { status: 204, statusText: 'No Content' });

  expect(completed).toBe(true);
  expect(errored).toBe(false);
});
```

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | TypeScript compilation | `cd web && npx ng build --configuration=production 2>&1; echo "EXIT:$?"` | exit 0 |
| G2 | Service unit tests | `cd web && npx vitest run --reporter=verbose src/app/features/tasks/data-access/task.service.spec.ts 2>&1; echo "EXIT:$?"` | exit 0, all tests pass including 2 new deleteTask tests |
| G3 | Delete method signature | Code review: `deleteTask(id: string): Observable<void>` uses `http.delete<void>()` — no `responseType: 'text'` override | verified |
| G4 | No optimistic delete | Code review: `onDelete` calls `taskService.deleteTask` INSIDE the subscribe callback it updates state — state update happens in `next:` callback, never before subscribe | verified |
| G5 | data-testid present | Code review: delete button has `[attr.data-testid]="'delete-btn-' + task.id"` | verified |
| G6 | Confirmation gate | Code review: `window.confirm()` or `confirm()` is called before `deleteTask` service call — early return on cancel | verified |
| G7 | Regression — existing tests | `cd web && npx vitest run --reporter=verbose 2>&1; echo "EXIT:$?"` | exit 0, all existing tests pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Create a custom modal/dialog component library — use `window.confirm()` only
- Add animations, transitions, or toast notifications for delete
- Add `responseType: 'text'` to the delete call unless you observe an actual interceptor JSON.parse failure
- Implement optimistic UI (removing before 204 confirmation)
- Add undo/trash/recovery functionality
- Add bulk delete UI
- Modify any backend file
- Write E2E tests (that is the next handoff)
- Create new component files — modify existing `task-list-stub` only

### SCOPE BOUNDARY — Stop when:

- `deleteTask` method added to `TaskService`
- Delete button visible per task row with correct `data-testid`
- `window.confirm` gates the delete call
- On 204, task removed from signal state (no refetch)
- All unit tests pass
- Do NOT proceed to E2E test work

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| Optimistic delete: removing from state before 204 | On 404/500 failure, UI is inconsistent with DB, no rollback defined | Remove from state ONLY in `next:` callback after 204 |
| `this.taskService.getTasks().subscribe(...)` after delete | Full refetch on every delete wastes a round-trip and resets scroll/selection state | `this.tasks.update(tasks => tasks.filter(t => t.id !== taskId))` — local state mutation |
| Custom modal component | Over-engineers the confirmation step beyond DOD scope | `window.confirm('...')` — functional, keyboard-accessible by default |
| `http.delete(url, { responseType: 'text' })` | Speculative fix for empty-body handling that Angular HttpClient already handles natively for `delete<void>()` | `http.delete<void>(url)` — no override needed |
| `(click)="deleteTask(task.id)"` calling service directly from template | Skips confirmation step entirely | Call `onDelete(task.id)` which wraps confirm + service call |

## 9. Rollback Guidance

1. If G1 fails: check imports — `Observable` type import may need explicit addition if not already re-exported
2. If G2 fails: verify `HttpTestingController` mock flushes with `{ status: 204, statusText: 'No Content' }` and null body
3. If G7 fails: check that existing tests still find their expected DOM elements — adding a button per row should not break existing selectors
4. If `onDelete` causes a runtime error in tests: ensure `window.confirm` is spied/mocked in component tests if any exist
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- New features require corresponding tests
- Breaking an existing test is a blocking issue
- Frontend: Vitest unit tests for services and components

### TASKFLOW-ANTI-DRIFT
- Angular 22.0.6, standalone components, Vitest
- No external UI libraries for confirmation dialogs
- data-testid attributes required for E2E automation

### TASKFLOW-BUILD-PIPELINE
- All dependency versions pinned — do NOT add new packages
- Zod 4.4.3 for validation schemas (not needed for this task)

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B3-04
FILES_CREATED: []
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G3 no responseType override; confirm G4 no optimistic delete; confirm G5 data-testid; confirm G6 confirmation gate}
```
