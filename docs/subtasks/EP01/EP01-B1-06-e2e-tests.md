# Handoff: EP01-B1-06 — E2E: CreateTask Happy Path + Validation

## 1. Metadata

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | EP01-B1-06                                |
| Task Name     | E2E: CreateTask Happy Path + Validation   |
| Batch         | 1 of N (EP01 Chunk-C)                     |
| Epic          | EP01 — Task Management                    |
| User Stories  | US-004 (AC-004.1, AC-004.2, AC-004.3, AC-004.4) |
| Persona       | QA Automation — Playwright E2E            |
| Model Tier    | sonnet                                    |

## 2. Objective

Write and pass 3 Playwright E2E specs proving CreateTask works end-to-end through the real UI against a real backend + PostgreSQL. Since US-005 (List Tasks) may not exist yet, this task first checks for an existing list component; if absent, it builds a minimal, clearly-temporary stub list view so the "appears in list" assertion is not blocked.

## 3. Pre-Conditions

- [ ] EP01-B1-04b and EP01-B1-05b both report STATUS: DONE
- [ ] Full stack runs locally: API (with PostgreSQL via Docker) + Angular dev server both start successfully
- [ ] Playwright is installed and configured in the `e2e/` directory
- [ ] Playwright config's `baseURL` and `webServer` commands point at correct API + FE ports
- [ ] **CHECK (run before starting work)**: Does a real task-list component exist at `web/src/app/features/tasks/task-list/`? Run `find web/src/app/features/tasks/ -name "*task-list*" -o -name "*list*"` to check.
  - If YES (US-005 exists) — use the real list view for assertion, do NOT build a stub
  - If NO — build a minimal stub (see Deliverables, conditional section)

If the full stack cannot start, report BLOCKED.

## 4. Context Bundle

| File                                                       | Lines     | Why                                          |
| ---------------------------------------------------------- | --------- | -------------------------------------------- |
| `docs/user-stories/US-004-create-task.md`                  | 29-79     | AC-004.1 through AC-004.4                    |
| `docs/user-stories/US-004-create-task.md`                  | 173-185   | Risks — LOW: E2E dependency on US-005        |
| `web/src/app/features/tasks/create-task/create-task.component.ts` | all | Component under test                         |
| `web/src/app/app.routes.ts`                                | all       | Current routing (to find/add list route)     |

## 5. Deliverables

### Files to Create

| File Path                                                       | Contents |
| --------------------------------------------------------------- | -------- |
| `e2e/src/tests/tasks/create-task.spec.ts`                       | 3 Playwright E2E specs |

### Conditional Files (ONLY if US-005 list component does NOT exist):

| File Path                                                                   | Contents |
| --------------------------------------------------------------------------- | -------- |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.ts`     | Minimal: GET /api/tasks, render task titles only |
| `web/src/app/features/tasks/task-list-stub/task-list-stub.component.html`   | Simple `<ul>` of task titles |

### Files to Modify (conditional)

| File Path                     | Change                                    |
| ----------------------------- | ----------------------------------------- |
| `web/src/app/app.routes.ts`   | ONLY if stub built: add `{ path: 'tasks-stub', component: TaskListStubComponent }` |

### Expected Signatures

```typescript
// create-task.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Create Task', () => {
  test('CreateTask_FromUI_AppearsInTaskList', async ({ page }) => {
    // Navigate to /tasks/create
    // Fill title, submit
    // Navigate to list (real /tasks or stub /tasks-stub)
    // Assert new task title visible in list
  });

  test('CreateTask_WithEmptyTitle_ShowsValidationErrorInUI', async ({ page }) => {
    // Navigate to /tasks/create
    // Leave title empty, click submit (or blur title field)
    // Assert inline validation error displayed
    // Assert NO network request was made (client-side blocking)
  });

  test('CreateTask_WithPastDueDate_ShowsValidationErrorInUI', async ({ page }) => {
    // Navigate to /tasks/create
    // Enter a past date
    // Assert inline validation error displayed
  });
});

// task-list-stub.component.ts (CONDITIONAL)
// TEMPORARY — superseded by US-005. Do NOT extend. Remove when US-005 lands.
@Component({ selector: 'app-task-list-stub', standalone: true, ... })
export class TaskListStubComponent { /* GET /api/tasks, render titles */ }
```

## 6. Quality Gates

| #  | Gate                          | Command                                                                 | Pass Criteria         |
| -- | ----------------------------- | ----------------------------------------------------------------------- | --------------------- |
| G1 | E2E suite                     | `npx playwright test e2e/src/tests/tasks/create-task.spec.ts`           | exit 0, 3 passed      |
| G2 | No route collision (if stub)  | If stub built: `app.routes.ts` has `/tasks-stub` distinct from `/tasks` | verified              |
| G3 | Stub marked temporary (if built) | Stub file contains "TEMPORARY" and "superseded by US-005"             | verified              |
| G4 | FE build still passes         | `cd web && npx ng build`                                                | exit 0                |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Build a full US-005 implementation (filtering, sorting, pagination) — stub renders titles only
- Reuse the `/tasks` route for the stub — must use distinctly named route
- Leave the stub undocumented as temporary — must carry TEMPORARY marker
- Write more than 3 E2E tests — thin E2E layer, heavy coverage is at unit/integration levels
- Test server-round-trip 400 handling in E2E and call it "client-side validation" — the two are distinct failure modes

### SCOPE BOUNDARY — Stop when:

- All 3 E2E specs pass
- If stub was built, it is clearly marked temporary with no route collision
- All quality gates pass
- This is the LAST task in the batch — report batch completion status

## 8. Anti-Patterns

| Anti-Pattern                                        | Why It Fails                                               | Do Instead                                  |
| --------------------------------------------------- | ---------------------------------------------------------- | ------------------------------------------- |
| Building stub without checking for US-005 first     | Risks route/component collision with real implementation    | Run the `find` check in Pre-Conditions      |
| Reporting BLOCKED due to missing US-005             | This is an acknowledged soft dependency — stub is the solution | Build the stub per this handoff            |
| Testing server 400 and calling it "client-side"     | Conflates two failure modes                                 | Assert no network call fires for client-side tests |
| Making the stub complex (filters, styling)          | Scope creep — US-005 will replace it entirely               | Minimal: titles only, no styling            |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed
2. If G1 fails on test #1 and no stub/list exists: check Pre-Conditions check was run — build the stub
3. If G1 fails on test #2/#3 (validation errors): check that the CreateTaskComponent from EP01-B1-05b actually renders inline errors for the tested field — the E2E test verifies the component works in a real browser
4. If G2 (route collision) fails: rename stub route to `/tasks-stub-temp`
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED. If the failure is that the full stack won't start (Docker, ports), report BLOCKED.

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
- Docker Compose: postgres:17.5, taskflow-api, taskflow-web

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B1-06
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {state whether real US-005 list existed or stub was built; if stub, state its route; this batch's DOD is now complete pending orchestrator sign-off}
```
