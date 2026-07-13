> [📚 INDEX](../INDEX.md) / [EP01 — Task Management](../epics/EP01-task-management.md) / US-007

# US-007 — Update Task

**Epic**: [EP01 - Task Management](../epics/EP01-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **update an existing task** so that **I can reflect changes in my work**.

## Definition of Ready

- [x] US-004 (Create Task) is implemented and merged -- Update Task depends on existing task entity, repository interfaces, and persistence logic.
- [x] Task entity schema confirmed: id (UUID v7), ownerId (UUID v7), title (string), description (string, nullable), status (enum: Pending | In Progress | Completed), dueDate (DateTime?, nullable), createdAt (DateTime), updatedAt (DateTime).
- [x] Hardcoded/seed ownerId strategy for Delivery 1 confirmed and documented -- AC-007.5 (404 for another user's task) is testable via a second seeded ownerId without auth.
- [x] Status enum values (Pending, In Progress, Completed) finalized and match Create Task story exactly -- no drift between US-004 and US-007.
- [x] Error shape contract confirmed: { status, error, message, details: [{ field, issue }] } -- applies to both 400 and 404 responses, implemented as shared middleware/filter.
- [x] Business decision confirmed and documented: past due dates are explicitly ALLOWED on update (differs from Create Task validation which rejects past dates per AC-004.4).
- [x] PATCH partial-update semantics agreed: request body fields are optional; a field's ABSENCE means 'do not touch', not 'set to null/default'. For Delivery 1, explicit null for description/dueDate is treated the same as omission (not a clear operation) -- explicit-null-clears-field deferred to a follow-up.
- [x] API contract (PATCH /api/tasks/{id}, request/response DTOs) documented in docs/architecture/api-contract.md section 4.4.
- [x] FluentValidation CascadeMode.Continue convention confirmed for partial-update validators (collect all field errors, not fail-fast).
- [x] Test data builder/seed for 'another user's task' fixture available or creatable in IntegrationTests project (needed for AC-007.5).

## Acceptance Criteria

- [x] **AC-007.1: Update title**
  - **Given** an existing task owned by the requesting (seed) user
  - **When** the title field is included in the PATCH payload with a new non-empty value
  - **Then** the task's title is updated, the response returns 200 with the full task object reflecting the new title, and updatedAt is refreshed to the current UTC time

- [x] **AC-007.2: Update status (free-form transition)**
  - **Given** an existing task owned by the requesting user and a valid status value (Pending, In Progress, or Completed)
  - **When** the status field is changed via PATCH, regardless of the current status (free-form transition, no state machine restriction)
  - **Then** the task's status is updated, the response returns 200 with the new status and updatedAt refreshed

- [x] **AC-007.3: Update description and/or dueDate (past date allowed)**
  - **Given** an existing task owned by the requesting user with description and/or dueDate included in the PATCH payload
  - **When** they are changed (including dueDate set to a past date, which is explicitly allowed on update unlike create)
  - **Then** the fields are updated, the response returns 200 with the new values and updatedAt refreshed; fields not included in the request remain unchanged

- [x] **AC-007.4: Invalid status enum value rejected**
  - **Given** a status value not in the enum (e.g. 'Done', 'Archived', empty string, numeric, or malformed casing)
  - **When** it is submitted via PATCH
  - **Then** the request is rejected with 400 and the details array includes field 'status' with an issue message listing the accepted enum values (Pending, In Progress, Completed)

- [x] **AC-007.5: 404 for another user's task or non-existent task**
  - **Given** a task owned by a different (seed) user, OR a task ID that does not exist at all (well-formed UUID)
  - **When** any PATCH is attempted against that task's id
  - **Then** a 404 is returned with the standard error shape -- the response body is structurally identical between both cases so no ownership information is disclosed

- [x] **AC-007.6: Empty or whitespace-only title rejected**
  - **Given** the title field is included in the PATCH payload but is empty string or whitespace-only
  - **When** PATCH is submitted
  - **Then** the request is rejected with 400 and details includes field 'title' with a non-empty validation issue

- [x] **AC-007.7: Empty payload rejected**
  - **Given** a PATCH payload contains no updatable fields at all (empty body {}, or body with only unrecognized fields)
  - **When** the request is submitted
  - **Then** the request is rejected with 400 indicating at least one field is required -- a no-op update is not a valid mutation

- [x] **AC-007.8: Malformed task id rejected with 400**
  - **Given** a task id in the route that is syntactically invalid (not a parseable UUID/GUID)
  - **When** PATCH is submitted to that id
  - **Then** a 400 (not 404) is returned indicating the id format is invalid, distinguishing malformed input from a valid-but-missing resource

## Definition of Done

- [x] All ACs (AC-007.1 through AC-007.8) pass with automated tests at the appropriate layer (Domain unit, Application unit, Integration, E2E).
- [x] PATCH /api/tasks/{id} implemented supporting true partial update semantics -- omitted fields are left untouched.
- [x] Response 200 returns the full task object reflecting only the changed fields, with updatedAt refreshed to current UTC time.
- [x] 400 responses list valid enum values for status in the details array when an invalid status is submitted.
- [x] 404 returned for both non-existent task IDs and tasks owned by another seed user -- response body is structurally identical between the two cases (no information leak).
- [x] FluentValidation validator configured with CascadeMode.Continue so multiple simultaneous validation failures return all errors in a single 400 response.
- [x] Domain entity exposes explicit update methods enforcing invariants -- Application layer never sets entity properties directly.
- [x] Ownership check reuses the exact same helper/specification used by prior task stories (US-005 Get, US-006 View Detail, US-008 Delete) -- single source of truth for 404-on-mismatch.
- [x] updatedAt set server-side (UtcNow) inside entity method or handler, never accepted from request payload.
- [x] Domain layer has zero new external package references introduced by this story.
- [x] E2E test UpdateTask_ChangeStatusFromUI_ReflectsInListImmediately passes against the running Angular + API stack.
- [x] No regression in US-004/US-005/US-006/US-008 (Create/List/Get/Delete) test suites.
- [ ] Code reviewed and merged to feature/EP01-task-management with green CI (unit + integration + e2e).

## Deliverables

- `src/TaskFlow.Domain/Entities/TaskItem.cs` -- add Update/Rename/ChangeStatus/UpdateDescription/Reschedule domain methods enforcing invariants; touch UpdatedAt internally
- `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommand.cs`
- `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandHandler.cs`
- `src/TaskFlow.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandValidator.cs` -- FluentValidation, CascadeMode.Continue
- `src/TaskFlow.API/Controllers/TasksController.cs` -- add PATCH /api/tasks/{id} action
- `src/TaskFlow.API/Contracts/Tasks/UpdateTaskRequest.cs` -- dedicated request DTO (not reusing Create DTO)
- `tests/TaskFlow.Domain.Tests/Entities/TaskItemUpdateTests.cs` -- domain invariant unit tests for update methods
- `tests/TaskFlow.Application.Tests/Tasks/Commands/UpdateTask/UpdateTaskCommandHandlerTests.cs`
- `tests/TaskFlow.Application.Tests/Tasks/Commands/UpdateTask/UpdateTaskCommandValidatorTests.cs`
- `tests/TaskFlow.IntegrationTests/Tasks/UpdateTaskEndpointTests.cs`
- `e2e/src/tests/update-task.spec.ts`

## Test Plan

| Test Name | AC | Assertion |
|-----------|-----|-----------|
| TaskItem_Rename_WithNonEmptyTitle_UpdatesTitleAndUpdatedAt | AC-007.1 | Domain entity: Task.Title equals new value and Task.UpdatedAt is refreshed to a later UTC timestamp than prior UpdatedAt |
| TaskItem_Rename_WithEmptyOrWhitespaceTitle_ThrowsDomainException | AC-007.6 | Domain invariant throws before persistence is attempted for both empty string and whitespace-only title |
| TaskItem_ChangeStatus_ToAnyValidValueFromAnyCurrentStatus_UpdatesStatus | AC-007.2 | Free-form transition succeeds for all 3x3 status pairs including reverse transitions (Completed to Pending) |
| UpdateTaskCommandValidator_WithInvalidStatusString_FailsWithValidValuesListed | AC-007.4 | Validation failure message enumerates Pending, In Progress, Completed as accepted values |
| UpdateTaskCommandValidator_WithEmptyTitleAndInvalidStatus_FailsWithBothErrorsInDetails | AC-007.4, AC-007.6 | CascadeMode.Continue returns both field errors in a single validation result, not just the first |
| UpdateTaskCommandValidator_WithNoFieldsProvided_Fails | AC-007.7 | Validator rejects a command where title, description, status, dueDate are all unset/omitted |
| UpdateTaskCommandHandler_ForTaskOwnedByAnotherUser_ThrowsNotFound | AC-007.5 | Handler applies same ownerId-mismatch-as-NotFound pattern reused from prior task handlers; repository.SaveChangesAsync NOT called |
| UpdateTaskCommandHandler_WithPartialFields_OnlyUpdatesSpecifiedFieldsPreservingRest | AC-007.1, AC-007.3 | Fields omitted from request retain their pre-update values; only specified fields change; updatedAt refreshed |
| UpdateTaskCommandHandler_WithPastDueDate_SucceedsWithoutValidationError | AC-007.3 | Past dueDate is accepted and persisted, unlike Create Task validation which rejects past dates |
| UpdateTask_WithNewTitle_Returns200WithTitleUpdated | AC-007.1 | Integration test: PATCH with only title returns 200, title matches new value, updatedAt is newer than before, other fields unchanged |
| UpdateTask_WithValidStatusTransitionAnyDirection_Returns200WithNewStatus | AC-007.2 | Integration test: status changes freely (including Completed to Pending) without state-machine guard rejecting transition |
| UpdateTask_WithPastDueDate_Returns200 | AC-007.3 | Integration test: PATCH with dueDate in the past succeeds (200), contrasts with create-time validation |
| UpdateTask_WithInvalidStatusEnumValue_Returns400 | AC-007.4 | Integration test: 400 with details[].field == 'status' and message lists valid enum values |
| UpdateTask_OwnedByAnotherUser_Returns404 | AC-007.5 | Integration test: PATCH on seeded task with different ownerId returns 404, no ownership information leaked |
| UpdateTask_NonExistentId_Returns404IdenticalToOwnerMismatch | AC-007.5 | Integration test: well-formed UUID with no matching row returns 404, response body structurally identical to ownership-mismatch 404 |
| UpdateTask_WithEmptyTitleString_Returns400 | AC-007.6 | Integration test: title='' returns 400 with details field 'title' |
| UpdateTask_WithWhitespaceOnlyTitle_Returns400 | AC-007.6 | Integration test: whitespace-only title treated as empty and rejected with 400 |
| UpdateTask_WithEmptyPayload_Returns400RequiresAtLeastOneField | AC-007.7 | Integration test: empty {} body rejected with 400, message indicates at least one field required |
| UpdateTask_WithPartialValidPayload_Returns200WithUpdatedFields | AC-007.1, AC-007.3 | Integration test: only supplied fields changed; omitted fields retain previous values; updatedAt refreshed |
| UpdateTask_WithMalformedGuidInRoute_Returns400 | AC-007.8 | Integration test: non-GUID id segment returns 400 (not 404), distinguishing malformed input from missing resource |
| UpdateTask_ChangeStatusFromUI_ReflectsInListImmediately | AC-007.2 | E2E test: status change via UI dropdown is reflected in the task list without manual page refresh |

## Validation Rules

- title: if provided, must be non-empty and non-whitespace-only after trimming. No max length constraint in this story (deferred to cross-cutting decision).
- status: if provided, must be one of 'Pending', 'In Progress', 'Completed' (exact string match). Invalid values rejected with 400 listing valid values.
- description: if provided, accepted as-is (nullable). For Delivery 1, explicit null and omission are both treated as 'do not touch' (explicit-null-clears-field deferred).
- dueDate: if provided, must be a valid ISO 8601 date/datetime. Past dates explicitly allowed on update (differs from US-004 create validation). For Delivery 1, explicit null and omission are both treated as 'do not touch'.
- id (route): must be a syntactically valid UUID/GUID. Malformed id returns 400, not 404.
- updatedAt: set exclusively server-side (UtcNow) on every successful update. Never accepted from request payload.
- Payload must contain at least one recognized updatable field (title, description, status, dueDate). Empty body or body with only unknown fields returns 400.
- FluentValidation CascadeMode.Continue: all field validation errors are aggregated and returned in a single 400 response, not fail-fast on first error.
- Ownership: task must match the requesting user's ownerId (hardcoded/seed in Delivery 1). Mismatch returns 404 identical to not-found.

## Risks

- CRITICAL: Past-date-allowed-on-update but past-date-forbidden-on-create is an asymmetric validation rule between US-004 and US-007 validators. If the same FluentValidation rule object or base class is reused between Create and Update validators, this WILL cause a regression. Mitigation: separate validators (not shared inheritance), explicit differentiating test.
- HIGH: Nullable-vs-omitted ambiguity for description/dueDate. System.Text.Json cannot natively distinguish an omitted JSON property from an explicit null using plain nullable types (string?, DateTime?). For Delivery 1, both are treated as 'do not touch', but this is a known simplification. If explicit-null-clears-field is needed later, a presence-tracking wrapper (Optional<T>, JsonElement?) will be required. Mitigation: document as known simplification, defer to follow-up.
- MEDIUM: 404-for-another-user's-task depends on hardcoded/seed ownerId in Delivery 1 with no real auth. This AC validates a repository-level filter against a static seed value, not actual auth. Must be re-verified functionally in Delivery 3 once JWT claims exist. Mitigation: write tests that validate the domain/query filter mechanism explicitly, not auth.
- MEDIUM: Free-form status transitions (no state machine) is a deliberate simplification allowing flows like Completed to Pending. Acceptable for MVP but must be flagged as a known simplification for Delivery 3 if business rules evolve. Mitigation: document as intentional design decision, not an oversight.
- MEDIUM: CascadeMode.Continue must be verified at both the property-level and validator-level configuration. If FluentValidation global config uses CascadeMode.Stop, per-validator Continue may not behave as expected. Mitigation: explicit multi-error test (AC-007.4 + AC-007.6 simultaneously).
- LOW: The empty-PATCH-body behavior (AC-007.7 as 400-reject) was a gap in the original AC set resolved during refinement. Confirm this team decision is documented before implementation starts.
- LOW: PATCH endpoint must use a dedicated request DTO (UpdateTaskRequest), not reusing the CreateTaskRequest, to avoid silently inheriting Create's required-field constraints and date validation rules.

## Out of Scope

- Auth/JWT-based ownership enforcement (Delivery 3) -- Delivery 1 uses hardcoded/seed ownerId only.
- Explicit-null-clears-field semantics for description/dueDate (requires Optional<T> wrapper or JsonElement-based presence tracking) -- deferred to follow-up; Delivery 1 treats null same as omission.
- State machine / status transition rules (e.g., cannot go from Completed back to Pending) -- explicitly free-form per engineering decision.
- Bulk update / multi-task PATCH.
- Optimistic concurrency control (ETag/If-Match/RowVersion) for concurrent edits -- not required for Delivery 1; last-write-wins is accepted behavior.
- Audit trail / update history log -- only updatedAt timestamp, no change log entity.
- PUT (full replace) semantics -- only PATCH (partial update) is in scope.
- Title/description max length validation -- not specified in API contract or story notes; deferred to a cross-cutting decision before adding constraints.
- SQL injection / Unicode round-trip testing -- cross-cutting concern handled at framework level (EF Core parameterized queries); not a per-story AC.
- Rate limiting or abuse protection on the PATCH endpoint.
- User entity, Login, Register (EP02).
- FE edit dialog/form implementation -- covered by separate FE story or as part of US-017 Task List View.

## Notes

- Partial updates should be supported (only send fields that changed)
- Due date validation: if provided, must be valid date (past dates allowed on update since a task might already be overdue)

## Related Documents

- [API Contract — Update Task](../architecture/api-contract.md#44-update-task--patch-apitasksid) — request/response shape and error codes
- [Testing Strategy — US-007 coverage](../architecture/testing-strategy.md#us-007--update-task-patch-apitasksid)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-006 — View Task Detail](US-006-view-task-detail.md) — where updated values are reflected
