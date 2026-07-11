> [📚 INDEX](../INDEX.md) / [EP01 — Task Management](../epics/EP01-task-management.md) / US-008

# US-008 — Delete Task

**Epic**: [EP01 - Task Management](../epics/EP01-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **delete a task** so that **I can remove tasks I no longer need**.

## Definition of Ready

- [ ] Task entity with OwnerId (UUID v7) exists from prior CRUD stories (US-004..US-007) and is populated via Guid.CreateVersion7() at insert time.
- [ ] ITaskRepository port exists in Domain layer with async lookup patterns established by prior stories.
- [ ] Hardcoded/seeded current-user GUID mechanism for Delivery 1 (no JWT) is defined and consistent across all Task CRUD stories. Seed data includes at least two distinct ownerId values (UUID v7) so cross-owner scenarios (AC-008.3) are testable.
- [ ] Standard error response shape { status, error, message, details: [] } is finalized and already used by at least one prior 404 path (GET/PATCH endpoints) for consistency.
- [ ] API contract for DELETE /api/tasks/{id} confirmed in docs/architecture/api-contract.md section 4.5: 204 No Content on success (empty body), 404 on all failure modes.
- [ ] Decision confirmed: hard delete only -- no soft-delete column (isDeleted/deletedAt), no audit/history table, no trash/recovery mechanism for this story.
- [ ] PostgreSQL Testcontainers integration test harness is available and running (no InMemory/SQLite substitutes).
- [ ] Prior CRUD stories (US-004..US-007) are merged so repository patterns, ownership-filtering query shape, and CQRS folder conventions are established and reusable.

## Acceptance Criteria

- [ ] **AC-008.1: Owned task is permanently deleted**
  - **Given** an existing task owned by the current user (ownerId matches the hardcoded/seeded Delivery 1 caller context)
  - **When** DELETE /api/tasks/{id} is called with that task's id
  - **Then** the task is permanently removed from the database (hard delete) and the API returns 204 No Content with an empty body

- [ ] **AC-008.2: Non-existent task returns 404**
  - **Given** a syntactically valid UUID that does not correspond to any existing task
  - **When** DELETE /api/tasks/{id} is called with that id
  - **Then** the API returns 404 with the standard error shape { status, error, message, details: [] } and no database records are modified

- [ ] **AC-008.3: Task owned by another user returns 404**
  - **Given** a task exists but belongs to a different ownerId than the current caller context (hardcoded/seeded in Delivery 1)
  - **When** DELETE /api/tasks/{id} is called with that task's id
  - **Then** the API returns 404 (never 403) with the standard error shape -- existence of another user's task must not be disclosed

- [ ] **AC-008.4: Repeated delete is idempotent**
  - **Given** a task that was already deleted by a prior successful DELETE call
  - **When** DELETE /api/tasks/{id} is called again with the same id
  - **Then** the API returns 404 with the standard error shape (not 500) and no unhandled exception is logged -- delete is idempotent from the client's perspective

- [ ] **AC-008.5: Success response has empty body and no Content-Type**
  - **Given** a successful deletion of an owned task
  - **When** the 204 response is returned to the client
  - **Then** the response has status 204, an empty body (no JSON payload), and no Content-Type: application/json header -- Content-Length is 0 or absent

## Definition of Done

- [ ] DELETE /api/tasks/{id} endpoint implemented end-to-end (API -> Application command handler -> Infrastructure repository -> PostgreSQL) returning 204 No Content with empty body on success.
- [ ] All 5 ACs (AC-008.1 through AC-008.5) have corresponding automated tests passing.
- [ ] Unit tests (Application layer) use mocked ITaskRepository -- zero real DB access in this layer.
- [ ] Integration tests run against real PostgreSQL via Testcontainers, asserting actual row removal via a follow-up query.
- [ ] E2E test DeleteTask_FromUI_RemovesFromListAndConfirms passes against the running full stack (Angular UI + API).
- [ ] All failure paths (not-found, not-owned, already-deleted) return 404 with the standard error shape { status, error, message, details: [] } -- never 500, never an unhandled exception.
- [ ] No soft-delete artifact leaked: no deletedAt/isDeleted field in entity, schema, or response contract.
- [ ] Domain project (TaskFlow.Domain) has zero new external package references introduced by this story.
- [ ] Clean Architecture boundary compliance verified: Application layer contains no EF Core types; Domain layer contains no Infrastructure types.
- [ ] Ownership check uses (Id + OwnerId) composite predicate at the repository level so Delivery 3 JWT wiring only swaps the ownerId source, not the query shape.
- [ ] No regression in existing Create/List/Get/Update task endpoints or their tests.
- [ ] Angular UI: delete action wired with a confirmation step (user must confirm before DELETE call fires); on 204 the task is removed from the list without a full page reload.

## Deliverables

- `src/TaskFlow.Application/Tasks/Commands/DeleteTask/DeleteTaskCommand.cs`
- `src/TaskFlow.Application/Tasks/Commands/DeleteTask/DeleteTaskCommandHandler.cs`
- `src/TaskFlow.Domain/Tasks/ITaskRepository.cs` (add DeleteAsync signature if not already present)
- `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` (implement DeleteAsync -- LINQ only, hard delete, (Id + OwnerId) composite predicate)
- `src/TaskFlow.API/Program.cs` or `Endpoints/TaskEndpoints.cs` (map DELETE /api/tasks/{id} -> 204/404)
- `tests/TaskFlow.Application.Tests/Tasks/Commands/DeleteTask/DeleteTaskCommandHandlerTests.cs`
- `tests/TaskFlow.IntegrationTests/Tasks/DeleteTaskEndpointTests.cs`
- `e2e/src/tests/tasks/delete-task.spec.ts`

## Test Plan

| Test Name | AC | Assertion |
|-----------|-----|-----------|
| DeleteTask_WithOwnedTask_Returns204AndRemovesRecord | AC-008.1 | Response status is 204 with empty body; record is permanently absent from PostgreSQL after the call (verified via direct query); response body has zero bytes. |
| DeleteTask_WithNonExistentId_Returns404 | AC-008.2 | Given a random valid UUID with no matching task row, DELETE returns HTTP 404 with error shape { status: 404, error, message, details: [] }; database row count is unchanged. |
| DeleteTask_OwnedByAnotherUser_Returns404 | AC-008.3 | Given a seeded task with ownerId=B and request context ownerId=A, DELETE returns HTTP 404 (not 403); the other user's task row still exists in the database after the call. |
| DeleteTask_CalledTwice_SecondCallReturns404NotServerError | AC-008.4 | Single test method: first DELETE returns 204 and removes the row; second DELETE with the same id returns 404 (not 500); no unhandled exception thrown. |
| DeleteTask_SuccessResponse_HasEmptyBodyAndNoContentType | AC-008.5 | On 204 response: Content-Length is 0 or absent; no Content-Type: application/json header present; response body stream has zero bytes. |
| DeleteTaskCommandHandler_TaskExistsAndOwned_CallsRepositoryDeleteAsyncOnce | AC-008.1 | Unit test with mocked ITaskRepository: DeleteAsync is invoked exactly once with the correct (id, ownerId) arguments and handler returns success result. |
| DeleteTaskCommandHandler_RepositoryReturnsFalse_ReturnsNotFoundResult | AC-008.2, AC-008.4 | When mocked repository returns false (row not found or already deleted), handler returns a not-found result (not an exception); maps to 404 at the API layer. |
| DeleteTask_FromUI_RemovesFromListAndConfirms | AC-008.1, AC-008.5 | E2E: user clicks delete on a rendered task row, confirms in the confirmation dialog, task disappears from the DOM list without a full page reload, no console errors. |

## Validation Rules

- Unit tests (DeleteTaskCommandHandlerTests) run in tests/TaskFlow.Application.Tests/ with mocked ITaskRepository via NSubstitute -- validate handler logic for AC-008.1 through AC-008.4 without DB access.
- Integration tests (DeleteTaskEndpointTests) run in tests/TaskFlow.IntegrationTests/ against real PostgreSQL via Testcontainers -- validate HTTP response codes, error shapes, and actual row deletion for AC-008.1 through AC-008.5.
- E2E test (delete-task.spec.ts) runs in e2e/src/tests/ via Playwright against the full Angular + API stack -- validates AC-008.1 and AC-008.5 from the user's perspective.
- All 404 responses must match the standard error shape { status, error, message, details: [] } -- recommend a shared assertion helper in IntegrationTests to prevent drift across US-006/US-007/US-008.
- Repository DeleteAsync must use a composite predicate (Id AND OwnerId) in a single query -- never two separate queries (existence then ownership) to avoid TOCTOU race conditions.

## Risks

- Ownership-check ACs rely on hardcoded/seeded ownerId in Delivery 1 -- if test fixtures do not include two distinct ownerId values, AC-008.3 tests will pass vacuously. Mitigation: DOR requires dual-ownerId seed data.
- Hard delete is irreversible with no audit trail -- acceptable per explicit product decision, but if a future story requires deletion history or recovery, this design needs revisiting.
- AC-008.2 and AC-008.4 collapse to the same code path (both 404, no soft-delete marker) -- acceptable for hard delete, but must be explicitly tested as separate scenarios to prevent regression if persistence strategy changes.
- If prior stories used exception-driven control flow (throwing NotFoundException) for not-found cases, DeleteTaskCommandHandler must align with that convention -- diverging to a bool/Result pattern only for delete creates inconsistency. Align with whatever pattern US-006/US-007 established.
- EF Core's Remove() + SaveChangesAsync() on a non-tracked entity throws DbUpdateConcurrencyException -- repository must pre-check existence via the (Id + OwnerId) query or use ExecuteDeleteAsync with affected-rows check to avoid surfacing 500 for AC-008.4.
- 204 No Content with empty body: some HTTP clients or interceptors may mishandle empty-body responses (e.g., attempting JSON.parse on empty string) -- Angular HttpClient must be configured with appropriate responseType or the interceptor chain must tolerate empty bodies.
- Deadline pressure (2026-07-13, today 2026-07-11): US-008 is small but blocked on shared infrastructure (error-shape middleware, repository patterns) landing from prior stories first.

## Out of Scope

- Soft delete, trash/recycle bin, undo-delete, or recovery functionality.
- Audit trail or deletion history persistence (only runtime logging, not a product feature).
- Bulk delete or delete-multiple-tasks endpoint.
- Cascading delete of related entities (none exist for Task in Delivery 1 schema).
- Ownership enforcement via JWT claim -- Delivery 1 uses hardcoded/seeded ownerId only; real JWT-based ownership is Delivery 3.
- True concurrent-request race-condition handling (only sequential idempotency is covered by AC-008.4).
- Rate limiting or abuse prevention on the DELETE endpoint.
- Malformed GUID route-parameter handling as a story-specific AC -- this is ASP.NET Core default route-constraint behavior and should be addressed as a cross-cutting concern across all {id} endpoints.
- UI confirmation-dialog visual design polish beyond a functional confirm/cancel interaction.
- Cross-tab or cross-session real-time sync of deletions (e.g., via WebSocket or SSE).

## Notes

- Hard delete (permanent removal), no soft delete needed for this scope

## Related Documents

- [API Contract — Delete Task](../architecture/api-contract.md#45-delete-task--delete-apitasksid) — request/response shape and error codes
- [Testing Strategy — US-008 coverage](../architecture/testing-strategy.md#us-008--delete-task-delete-apitasksid)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-005 — List Tasks](US-005-list-tasks.md) — where deletion is reflected
