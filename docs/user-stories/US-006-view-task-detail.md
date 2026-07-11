> [📚 INDEX](../INDEX.md) / [EP01 — Task Management](../epics/EP01-task-management.md) / US-006

# US-006 — View Task Detail

**Epic**: [EP01 - Task Management](../epics/EP01-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **view the full details of a specific task** so that **I can see all its information**.

## Definition of Ready

- [ ] Full response field list confirmed per api-contract.md Section 4.3: id, title, description (nullable), status, dueDate (nullable), ownerId, createdAt, updatedAt
- [ ] TaskDetailDto field set confirmed as distinct from TaskListItemDto (8 fields vs 4 fields)
- [ ] 404-not-403 policy for cross-owner access confirmed as deliberate anti-enumeration design per api-contract.md: 'callers cannot distinguish does not exist from not yours'
- [ ] Implementation strategy confirmed: single combined query WHERE Id = @id AND OwnerId = @ownerId returning null for both not-found and not-owned cases (no two-step lookup)
- [ ] ITaskRepository.GetByIdForOwnerAsync(Guid id, Guid ownerId, CancellationToken ct) signature agreed
- [ ] Malformed GUID route parameter handling decided: return 400 with details:[{field:'id', issue:'must be a valid GUID'}] -- distinct from 404 for well-formed-but-missing IDs
- [ ] Seed data includes at least one task with description=null and dueDate=null, plus one with both set, for null-handling test coverage
- [ ] Seed data includes at least one task per distinct ownerId for cross-owner 404 test
- [ ] createdAt/updatedAt format confirmed as UTC ISO 8601 timestamps; description serializes as JSON null (not empty string) when unset

## Acceptance Criteria

- [ ] **AC-006.1: Full detail returned for owned task**
  - **Given** a task owned by the seeded caller exists
  - **When** requesting GET /api/tasks/{id} with that task's id
  - **Then** returns HTTP 200 with id, title, description, status, dueDate, ownerId, createdAt, updatedAt -- description and dueDate render as JSON null when unset, never omitted

- [ ] **AC-006.2: Non-existent id returns 404**
  - **Given** a syntactically valid GUID that does not correspond to any existing task
  - **When** requesting GET /api/tasks/{id}
  - **Then** returns HTTP 404 with standard error shape; message does not reveal whether the id ever existed

- [ ] **AC-006.3: Cross-owner access returns 404, not 403**
  - **Given** a task exists but is owned by a different seeded user
  - **When** requesting GET /api/tasks/{id} as the non-owning caller
  - **Then** returns HTTP 404 (not 403) with error body identical in shape to AC-006.2, preventing existence enumeration

- [ ] **AC-006.4: Malformed GUID route parameter returns 400**
  - **Given** the {id} route parameter is not a syntactically valid GUID (e.g., 'abc123', '123', empty string)
  - **When** requesting GET /api/tasks/{id}
  - **Then** returns HTTP 400 with details:[{field:'id', issue:'must be a valid GUID'}] -- never 500, no stack trace

- [ ] **AC-006.5: updatedAt equals createdAt for unmodified task**
  - **Given** a task that has never been modified since creation
  - **When** retrieving its detail
  - **Then** updatedAt equals createdAt exactly (both set at creation time)

- [ ] **AC-006.6: Optional fields serialize as null, not empty string**
  - **Given** a task created without a description or dueDate
  - **When** retrieving its detail
  - **Then** description and dueDate serialize as JSON null (not empty string, not omitted field), response is HTTP 200

## Definition of Done

- [ ] GetTaskByIdQueryHandler implemented and unit tested against NSubstitute-mocked ITaskRepository
- [ ] TaskRepository.GetByIdForOwnerAsync implemented as single LINQ predicate Where(t => t.Id == id && t.OwnerId == ownerId) and integration-tested against real PostgreSQL
- [ ] GET /api/tasks/{id} returns full TaskDetailDto shape on success (8 fields)
- [ ] Non-existent id and another-owner's id both return 404 with identical response shape -- no distinguishing signal between the two cases (verified by response-body-equality assertion)
- [ ] Malformed GUID route parameter returns 400 with field-level details for 'id' -- never 500, no stack trace
- [ ] description and dueDate serialize as JSON null when unset, never as empty string and never omitted from the response
- [ ] NotFoundException mapped to 404 via ExceptionHandlingMiddleware with standard error shape
- [ ] Swagger/OpenAPI updated for GET /api/tasks/{id}
- [ ] No InMemory/SQLite provider used in tests
- [ ] Code reviewed and merged to feature branch

## Deliverables

- `src/TaskFlow.Domain/Repositories/ITaskRepository.cs` (add GetByIdForOwnerAsync signature)
- `src/TaskFlow.Application/Tasks/Queries/GetTaskById/GetTaskByIdQuery.cs`
- `src/TaskFlow.Application/Tasks/Queries/GetTaskById/GetTaskByIdQueryHandler.cs`
- `src/TaskFlow.Application/Tasks/Dtos/TaskDetailDto.cs`
- `src/TaskFlow.Application/Common/Exceptions/NotFoundException.cs`
- `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` (GetByIdForOwnerAsync implementation)
- `src/TaskFlow.API/Controllers/TasksController.cs` (GET /api/tasks/{id} action)
- `src/TaskFlow.API/Middleware/ExceptionHandlingMiddleware.cs` (maps NotFoundException to 404)
- `tests/TaskFlow.Application.Tests/Tasks/Queries/GetTaskByIdQueryHandlerTests.cs`
- `tests/TaskFlow.IntegrationTests/Tasks/GetTaskByIdEndpointTests.cs`

## Test Plan

| Test Name | AC | Assertion |
|-----------|-----|-----------|
| GetTaskById_WithOwnedTask_Returns200WithFullDetail | AC-006.1 | response body contains all 8 contracted fields with correct values matching seeded entity |
| GetTaskById_WithNonExistentId_Returns404 | AC-006.2 | a random well-formed GUID not present in DB returns 404 with standard error shape |
| GetTaskById_OwnedByAnotherUser_Returns404NotForbidden | AC-006.3 | status is exactly 404 (never 403); response body identical in shape to the non-existent-id case (no distinguishing field or message) |
| GetTaskById_WithMalformedGuid_Returns400 | AC-006.4 | ids 'abc', '123' each return 400 with details containing field == 'id'; never 500; no stack trace in body |
| GetTaskById_NeverModified_UpdatedAtEqualsCreatedAt | AC-006.5 | a freshly seeded, unmodified task returns updatedAt == createdAt |
| GetTaskById_WithoutOptionalFields_ReturnsNullNotEmptyString | AC-006.6 | a task seeded with description=null and dueDate=null returns JSON null for both fields, not empty string and not field omission |
| GetTaskById_CrossOwnerTask_Returns404WithIdenticalBodyToNonExistent | AC-006.3 | two HTTP calls (non-existent id, cross-owner id) return byte-identical error response bodies aside from request-specific metadata |
| GetTaskById_Repository_GeneratesSingleQuery | AC-006.1 | EF Core generates exactly one SQL SELECT with combined WHERE (Id AND OwnerId), no separate EXISTS check |

## Validation Rules

- Route parameter {id} must be a syntactically valid GUID; malformed values return 400 with field 'id'
- Well-formed GUID not found in DB returns 404 via NotFoundException (never 500)
- Task owned by another user returns 404 identical to not-found (single-predicate query: WHERE Id = @id AND OwnerId = @ownerId)
- Response body for 404 must be identical in shape regardless of whether the task does not exist or is owned by another user (anti-enumeration)
- description serializes as JSON null when unset, never as empty string; empty string and null are distinct states
- dueDate serializes as JSON null when unset, using date-only format (yyyy-MM-dd) when present

## Risks

- Malformed-GUID handling: default ASP.NET Core route constraints may return a generic 404 from routing before reaching the handler, bypassing the 400 contract -- must use explicit route validation (custom filter or model binder) to intercept malformed GUIDs and return the standard error shape
- Two-step lookup pattern (find by id, then check owner) is a latent enumeration and timing leak -- must enforce single-predicate query Where(t => t.Id == id && t.OwnerId == ownerId) in code review
- 404-not-403 guarantee is only as strong as response-body parity -- if error middleware attaches different message text for 'not found' vs 'not owned', it defeats anti-enumeration; needs explicit body-equality assertion, not just status-code check
- Because ownership filtering is not JWT-driven yet, AC-006.3 relies on seeded ownerId comparison -- must be re-verified end-to-end once JWT lands in Delivery 3
- Timestamp precision mismatch (DB truncates to microseconds, .NET DateTime to ticks) could cause updatedAt == createdAt assertion to flake if not comparing via the same serialization path

## Out of Scope

- 403 Forbidden responses -- deliberately excluded per AC-006.3 anti-enumeration policy
- Edit/Update/Delete actions on individual tasks (separate stories US-007, US-008)
- Resolving ownerId to a user display name or email (User entity is EP02/Delivery 2)
- Soft-delete or archived task detail retrieval
- Field-level partial responses (e.g., ?fields=title,status)
- Timing-attack mitigation (constant-time lookup) -- flagged as known risk, not required for Delivery 1

## Notes

- Return not-found (not forbidden) for tasks belonging to other users to prevent enumeration attacks

## Related Documents

- [API Contract — View Task Detail](../architecture/api-contract.md#43-view-task-detail--get-apitasksid) — request/response shape and error codes
- [Testing Strategy — US-006 coverage](../architecture/testing-strategy.md#us-006--view-task-detail-get-apitasksid)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-005 — List Tasks](US-005-list-tasks.md) — entry point that links to task detail
