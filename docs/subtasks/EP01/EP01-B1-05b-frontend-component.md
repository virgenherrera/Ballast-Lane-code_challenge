# Handoff: EP01-B1-05b — Frontend: CreateTaskComponent (Reactive Form + Tailwind UI)

## 1. Metadata

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | EP01-B1-05b                               |
| Task Name     | Frontend: CreateTaskComponent (Reactive Form + Tailwind UI) |
| Batch         | 1 of N (EP01 Chunk-C)                     |
| Epic          | EP01 — Task Management                    |
| User Stories  | US-004 (AC-004.1, AC-004.2, AC-004.3, AC-004.4, AC-004.8, AC-004.9) |
| Persona       | Angular Presentation Engineer             |
| Model Tier    | sonnet                                    |

## 2. Objective

Implement `CreateTaskComponent`: a routed page with a reactive form consuming EP01-B1-05a's `TaskService` and Zod schema, client-side validation including an NBSP-aware `notBlankValidator`, loading state during submit, field-level error rendering from the API error mapper, form reset on success, and Tailwind-styled UI approximating the TailAdmin aesthetic. Also produces a reusable `FormFieldComponent` wrapper for US-005/006/007 forms.

## 3. Pre-Conditions

- [ ] EP01-B1-05a reports STATUS: DONE
- [ ] `TaskService`, `create-task.schema.ts`, `api-error-mapper.ts`, `task.constants.ts` all exist and pass tests
- [ ] `cd web && npx ng build` exits 0
- [ ] **DECISION (frozen)**: CreateTaskComponent renders as a **dedicated routed page** at `/tasks/create` — not a modal. This decision is reused by US-006/US-007 edit forms.
- [ ] TailAdmin reference: form styling follows TailAdmin's input/textarea/button patterns via Tailwind utility classes only — no TailAdmin JS/component library import

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File                                                       | Lines     | Why                                          |
| ---------------------------------------------------------- | --------- | -------------------------------------------- |
| `docs/user-stories/US-004-create-task.md`                  | 29-96     | AC-004.1 through AC-004.10, DOD FE bullet   |
| `web/src/app/features/tasks/data-access/task.service.ts`   | all       | Service this component injects               |
| `web/src/app/features/tasks/data-access/create-task.schema.ts` | all   | Validation schema to mirror in form          |
| `web/src/app/shared/utils/api-error-mapper.ts`             | all       | Maps API 400 to field errors                 |
| `web/src/app/features/tasks/models/task.constants.ts`      | all       | TITLE_MAX_LENGTH, DESCRIPTION_MAX_LENGTH     |

## 5. Deliverables

### Files to Create

| File Path                                                                        | Contents |
| -------------------------------------------------------------------------------- | -------- |
| `web/src/app/features/tasks/create-task/create-task.component.ts`                | Reactive form, injects TaskService, submit with loading state |
| `web/src/app/features/tasks/create-task/create-task.component.html`              | Tailwind form: title, description textarea, dueDate, submit, inline errors |
| `web/src/app/features/tasks/create-task/create-task.component.spec.ts`           | 6 component tests |
| `web/src/app/shared/validators/not-blank.validator.ts`                           | Custom Angular validator rejecting whitespace/NBSP-only |
| `web/src/app/shared/validators/not-blank.validator.spec.ts`                      | 4 validator tests |
| `web/src/app/shared/ui/form-field/form-field.component.ts`                       | Reusable Tailwind input/error wrapper |
| `web/src/app/shared/ui/form-field/form-field.component.html`                     | Template with error slot |

### Files to Modify

| File Path                          | Change                                    |
| ---------------------------------- | ----------------------------------------- |
| `web/src/app/app.routes.ts`        | Add route: `{ path: 'tasks/create', component: CreateTaskComponent }` |

### Expected Signatures

```typescript
// not-blank.validator.ts
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
export function notBlankValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').replace(/[\s ]+/g, '');
    return value.length === 0 ? { notBlank: true } : null;
  };
}

// create-task.component.ts
@Component({ selector: 'app-create-task', ... })
export class CreateTaskComponent {
  private readonly taskService = inject(TaskService);
  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    title: ['', [Validators.required, notBlankValidator(), Validators.maxLength(TITLE_MAX_LENGTH)]],
    description: ['', [Validators.maxLength(DESCRIPTION_MAX_LENGTH)]],
    dueDate: ['', [futureDateValidator()]], // exclusive: > now, same semantics as BE
  });

  isLoading = signal(false);
  fieldErrors = signal<FieldErrors>({});

  onSubmit(): void { /* set loading, call taskService.createTask, handle success (reset) and error (map) */ }
}
```

**Required Component Tests (6):**
1. `CreateTaskComponent_SubmitWithEmptyTitle_ShowsValidationError`
2. `CreateTaskComponent_SubmitWithWhitespaceOrNbspTitle_ShowsValidationError`
3. `CreateTaskComponent_SubmitWithPastDueDate_ShowsValidationError`
4. `CreateTaskComponent_SubmitDisabledWhileInvalid`
5. `CreateTaskComponent_SubmitSuccess_ResetsFormAndShowsLoadingState`
6. `CreateTaskComponent_ApiReturns400_MapsDetailsToFieldLevelErrors`

**Required Validator Tests (4):**
1. `notBlankValidator_WithEmptyString_ReturnsError`
2. `notBlankValidator_WithSpacesOnly_ReturnsError`
3. `notBlankValidator_WithNbspOnly_ReturnsError`
4. `notBlankValidator_WithValidText_ReturnsNull`

## 6. Quality Gates

| #  | Gate                          | Command                                                                            | Pass Criteria         |
| -- | ----------------------------- | ---------------------------------------------------------------------------------- | --------------------- |
| G1 | Build                         | `cd web && npx ng build`                                                           | exit 0                |
| G2 | Component tests               | `cd web && npx ng test --include='**/create-task.component.spec.ts' --watch=false` | exit 0, 6 passed      |
| G3 | Validator tests               | `cd web && npx ng test --include='**/not-blank.validator.spec.ts' --watch=false`   | exit 0, 4 passed      |
| G4 | No TailAdmin JS import        | Search `web/package.json` for "tailadmin" — must find zero matches                 | verified              |
| G5 | Route registered              | `web/src/app/app.routes.ts` contains path `'tasks/create'`                         | verified              |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Modify TaskService, Zod schema, or error mapper — frozen from EP01-B1-05a
- Import any TailAdmin JS/component library — Tailwind CSS utility classes only
- Build a generic form library beyond the single FormFieldComponent
- Invent the modal-vs-route decision — it is frozen as a routed page at `/tasks/create`
- Add navigation logic or breadcrumbs — just the form page itself

### SCOPE BOUNDARY — Stop when:

- All deliverables exist, all 10 tests (6 component + 4 validator) pass
- All quality gates pass
- Do NOT proceed to E2E work

## 8. Anti-Patterns

| Anti-Pattern                                           | Why It Fails                                                 | Do Instead                                    |
| ------------------------------------------------------ | ------------------------------------------------------------ | --------------------------------------------- |
| Relying on `Validators.required` alone for title       | Does not reject whitespace/NBSP-only — round-trips to server | Add `notBlankValidator()`                     |
| Using `>=` instead of `>` for dueDate comparison       | Silent FE/BE divergence on the exclusive boundary             | Mirror exact `> now` from BE                  |
| Building bespoke markup for each form field            | Duplicates styling for US-005/006/007 forms                   | Use `FormFieldComponent` wrapper              |
| Calling TaskService without loading state              | User sees no feedback during submit                           | Set `isLoading` signal before/after call      |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed
2. If G2 test #6 (error mapping) fails: check that the component reads `fieldErrors` signal correctly from `api-error-mapper.ts`'s output shape — do not modify the mapper, only the component's consumption
3. If G3 (validator) fails on NBSP: verify the regex `[\s ]+` correctly strips NBSP
4. If G5 fails: add the route entry to `app.routes.ts`
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
TASK: EP01-B1-05b
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm routed-page pattern applied at /tasks/create}
```
