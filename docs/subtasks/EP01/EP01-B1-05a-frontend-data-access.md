# Handoff: EP01-B1-05a — Frontend: TaskService, Model, Zod Schema, Error Mapper, Environment Config

## 1. Metadata

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | EP01-B1-05a                               |
| Task Name     | Frontend: TaskService, Model, Zod Schema, Error Mapper, Environment Config |
| Batch         | 1 of N (EP01 Chunk-C)                     |
| Epic          | EP01 — Task Management                    |
| User Stories  | US-004 (AC-004.3, AC-004.4, AC-004.6, AC-004.7, AC-004.8) |
| Persona       | Angular Data-Access Engineer              |
| Model Tier    | sonnet                                    |

**Split rationale**: Frontend Engineer role flagged the originally-proposed single FE task (8 files + validation + loading + error mapping) as exceeding the ~300-line cohesive-unit guideline. Split into 05a (data-access layer, testable with HttpTestingController) and 05b (presentation).

## 2. Objective

Implement the Angular data-access layer for task creation: `TaskService` (HTTP client wrapping POST /api/tasks), `task.model.ts`, `task.constants.ts` (shared validation limits), `create-task.schema.ts` (Zod 4 schema mirroring BE validation), `api-error-mapper.ts` (standard error shape to field-level errors), and environment configuration. All independently testable via `HttpTestingController`, with zero UI/component code.

## 3. Pre-Conditions

- [ ] EP01-B1-04b reports STATUS: DONE
- [ ] `docs/architecture/openapi-snapshots/create-task-201.json` exists (frozen contract fixture)
- [ ] `cd web && npx ng build` (or `npm run build`) exits 0
- [ ] `zod` 4.4.3 is already in `web/package.json` dependencies (confirmed present)
- [ ] Angular 22.0.6 with HttpClient available

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File                                                       | Lines     | Why                                          |
| ---------------------------------------------------------- | --------- | -------------------------------------------- |
| `docs/user-stories/US-004-create-task.md`                  | 13-27     | DOR — Zod schema mirrors BE validation       |
| `docs/user-stories/US-004-create-task.md`                  | 161-171   | Validation Rules — exact boundaries          |
| `docs/architecture/api-contract.md`                        | 64-83     | Standard error shape (section 2.3)           |
| `docs/architecture/api-contract.md`                        | 220-258   | POST /api/tasks contract (section 4.1)       |
| `docs/architecture/openapi-snapshots/create-task-201.json` | all       | Frozen response shape for Zod schema tests   |

## 5. Deliverables

### Files to Create

| File Path                                                             | Contents |
| --------------------------------------------------------------------- | -------- |
| `web/src/app/features/tasks/models/task.model.ts`                    | TypeScript interfaces: `Task`, `CreateTaskRequest`, `TaskResponse` |
| `web/src/app/features/tasks/models/task.constants.ts`                | `TITLE_MAX_LENGTH = 200`, `DESCRIPTION_MAX_LENGTH = 2000` — single source |
| `web/src/app/features/tasks/data-access/task.service.ts`             | Injectable service: `createTask(request): Observable<TaskResponse>` |
| `web/src/app/features/tasks/data-access/task.service.spec.ts`        | Unit tests with HttpTestingController |
| `web/src/app/features/tasks/data-access/create-task.schema.ts`       | Zod 4 schema mirroring BE validation rules |
| `web/src/app/features/tasks/data-access/create-task.schema.spec.ts`  | Schema validation tests |
| `web/src/app/shared/utils/api-error-mapper.ts`                       | Maps `{status, error, message, details[]}` to per-field error map |
| `web/src/app/shared/utils/api-error-mapper.spec.ts`                  | Error mapper unit tests |
| `web/src/environments/environment.ts`                                | `export const environment = { apiBaseUrl: 'http://localhost:5050' }` |
| `web/src/environments/environment.prod.ts`                           | `export const environment = { apiBaseUrl: '/api' }` (or production URL) |

### Files to Modify

None.

### Expected Signatures

```typescript
// task.constants.ts
export const TITLE_MAX_LENGTH = 200;
export const DESCRIPTION_MAX_LENGTH = 2000;

// task.service.ts
@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  createTask(request: CreateTaskRequest): Observable<TaskResponse> {
    return this.http.post<TaskResponse>(`${environment.apiBaseUrl}/api/tasks`, request);
  }
}

// create-task.schema.ts (Zod 4)
import { z } from 'zod';
import { TITLE_MAX_LENGTH, DESCRIPTION_MAX_LENGTH } from '../models/task.constants';

export const createTaskSchema = z.object({
  title: z.string()
    .transform(s => s.trim())
    .check(s => s.length > 0, { message: 'title required' })
    .check(s => s.length <= TITLE_MAX_LENGTH, { message: `title must not exceed ${TITLE_MAX_LENGTH} characters` }),
  description: z.string().max(DESCRIPTION_MAX_LENGTH).optional(),
  dueDate: z.string().datetime().optional()
    .check(d => !d || new Date(d) > new Date(), { message: 'must be future' }),
});

// api-error-mapper.ts
export interface ApiError { status: number; error: string; message: string; details: Array<{field: string; issue: string}>; }
export type FieldErrors = Record<string, string>;
export function mapApiErrorToFieldErrors(error: ApiError): FieldErrors { /* ... */ }
```

**Required TaskService Tests:**
1. `TaskService_createTask_PostsToCorrectEndpoint`
2. `TaskService_createTask_SendsRequestBodyCorrectly`

**Required Schema Tests:**
1. `createTaskSchema_WithEmptyTitle_Fails`
2. `createTaskSchema_WithWhitespaceOnlyTitle_Fails`
3. `createTaskSchema_WithNbspOnlyTitle_Fails` — title = `" "`
4. `createTaskSchema_WithTitleExceeding200Chars_Fails`
5. `createTaskSchema_WithTitleExactly200Chars_Passes`
6. `createTaskSchema_WithPastDueDate_Fails`
7. `createTaskSchema_WithValidPayload_Passes`

**Required Error Mapper Tests:**
1. `mapApiErrorToFieldErrors_WithSingleFieldError_MapsCorrectly`
2. `mapApiErrorToFieldErrors_WithMultipleFieldErrors_MapsAllFields` — AC-004.6 shape
3. `mapApiErrorToFieldErrors_WithEmptyDetails_ReturnsEmptyMap`

## 6. Quality Gates

| #  | Gate                          | Command                                                                     | Pass Criteria         |
| -- | ----------------------------- | --------------------------------------------------------------------------- | --------------------- |
| G1 | Build                         | `cd web && npx ng build`                                                    | exit 0                |
| G2 | TaskService tests             | `cd web && npx ng test --include='**/task.service.spec.ts' --watch=false`   | exit 0, all passing   |
| G3 | Zod schema tests              | `cd web && npx ng test --include='**/create-task.schema.spec.ts' --watch=false` | exit 0, includes NBSP case |
| G4 | Error mapper tests            | `cd web && npx ng test --include='**/api-error-mapper.spec.ts' --watch=false`| exit 0                |
| G5 | No duplicated constants       | Search web/src for literal `200` or `2000` used as validation limits outside `task.constants.ts` — must find zero | verified |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Create `CreateTaskComponent`, any `.html` template, or Tailwind styling — EP01-B1-05b
- Make any real HTTP call in `.spec.ts` files — `HttpTestingController` only
- Duplicate `TITLE_MAX_LENGTH`/`DESCRIPTION_MAX_LENGTH` as magic numbers — import from constants
- Decide modal-vs-routed-page presentation — EP01-B1-05b's Pre-Conditions
- Install any new npm packages — `zod` 4.4.3 is already present

### SCOPE BOUNDARY — Stop when:

- All 10 deliverable files exist, all quality gates pass
- Do NOT proceed to component/UI work

## 8. Anti-Patterns

| Anti-Pattern                                     | Why It Fails                                                 | Do Instead                                |
| ------------------------------------------------ | ------------------------------------------------------------ | ----------------------------------------- |
| Hardcoding 200/2000 in Zod schema                | Duplicates BE constants, risks silent drift                   | Import from `task.constants.ts`           |
| Testing TaskService against a live backend       | Slow, flaky, defeats HttpTestingController purpose            | Mock via HttpTestingController            |
| Skipping NBSP-specific schema test               | AC-004.3 explicitly calls out NBSP; needs explicit proof      | Add the named NBSP test case              |
| Using fetch() instead of Angular HttpClient      | Breaks Angular DI, interceptors, and testing patterns         | Use injected HttpClient                   |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed
2. If G3 (Zod schema) fails on NBSP: verify JavaScript `.trim()` handles NBSP (it does per spec) — the schema's transform must use `.trim()` not a regex that only strips ASCII whitespace
3. If G5 fails: find and replace the hardcoded numbers with imports from `task.constants.ts`
4. If contract fixture mismatch: do NOT silently adjust the Zod schema — flag to orchestrator that the API contract may have drifted
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

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
TASK: EP01-B1-05a
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {any issues or decisions made}
```
