> [📚 INDEX](../INDEX.md) / [EP01 — Task Management](../epics/EP01-task-management.md) / US-004

# US-004 — Create Task

**Epic**: [EP01 - Task Management](../epics/EP01-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **create a new task** so that **I can track work I need to do**.

## Definition of Ready

- [x] Task entity contract frozen and signed off by TL: id (Guid v7 via Guid.CreateVersion7), title (string, required, non-empty after trim, max 200 chars), description (string?, nullable, max 2000 chars), status (enum: Pending|InProgress|Completed), dueDate (DateTime? UTC, nullable), ownerId (Guid v7, required), createdAt (DateTime UTC), updatedAt (DateTime UTC)
- [x] API contract for POST /api/tasks frozen per docs/architecture/api-contract.md section 4.1: request { title, description?, dueDate? }, response 201 { id, title, description, status, dueDate, ownerId, createdAt, updatedAt } — no open questions on field names, types, or nullability
- [x] Error response shape { status, error, message, details: [{ field, issue }] } confirmed per docs/architecture/api-contract.md section 2.3 as the single contract for all 400 responses across all endpoints
- [x] Seed/hardcoded ownerId for Delivery 1 decided: a single named constant (SeedOwnerId) documented as UUID v7, referenced by ICurrentUserContext seed implementation, API default-owner logic, and all test fixtures — single source of truth until Delivery 3 JWT claim replaces it
- [ ] ICurrentUserContext abstraction and its Delivery-1 seed implementation (SeedCurrentUserContext) agreed as the mechanism for ownerId sourcing — no JWT dependency introduced, swappable in Delivery 3 without touching use case logic
- [x] Title max-length constraint confirmed at 200 characters (after trim); description max-length confirmed at 2000 characters — values documented in a shared Domain constants location before Domain entity tests are written
- [x] 'Future' dueDate boundary rule resolved: strictly greater than DateTime.UtcNow (exclusive — dueDate equal to 'now' is rejected as not-future). Comparison always uses server UTC time
- [x] FluentValidation CascadeMode.Continue confirmed as the project-wide validator configuration default — multiple validation failures in one request surface together in details[], not fail-fast on first rule
- [x] UUID v7 generation confirmed via Guid.CreateVersion7() available in .NET 10 runtime — Domain entity generates Id at construction time (not persistence time), no external NuGet package needed
- [x] EF Core PostgreSQL infrastructure confirmed: Npgsql.EntityFrameworkCore.PostgreSQL compatible with EF Core 10.0.4, Testcontainers.PostgreSql for integration tests — no InMemory/SQLite provider anywhere
- [ ] POST /api/tasks does NOT require an Authorization header in Delivery 1 — no 401 path testable yet; 401 AC explicitly deferred to Delivery 3, not silently dropped
- [x] Domain project (TaskFlow.Domain) has zero external PackageReferences — confirmed via TaskFlow.Domain.csproj; FluentValidation lives in Application only
- [x] FE date/time handling strategy agreed: browser date input converted to ISO 8601 UTC before submit; Zod schema mirrors BE validation (empty title, future-only dueDate)

## Acceptance Criteria

- [x] **AC-004.1: Successful task creation with default owner and status**
  - **Given** a user (Delivery 1: default seeded user via ICurrentUserContext) providing at least a non-empty title
  - **When** POST /api/tasks is submitted
  - **Then** a task is created with status "Pending", a server-generated UUID v7 id, and ownerId set from ICurrentUserContext; the 201 response contains the full task representation

- [x] **AC-004.2: Full task creation with all fields**
  - **Given** a request with title, description, and a valid future due date all provided
  - **When** POST /api/tasks is submitted
  - **Then** all three fields are persisted and returned exactly as submitted in the 201 response body

- [x] **AC-004.3: Title is required**
  - **Given** a request with no title, a null title, an empty string title, or a whitespace-only title (spaces, tabs, newlines, Unicode whitespace such as NBSP U+00A0)
  - **When** POST /api/tasks is submitted
  - **Then** the request is rejected with 400 and details contains { field: "title", issue: "title required" }; no task is persisted

- [x] **AC-004.4: Due date must be in the future**
  - **Given** a request with a dueDate strictly in the past relative to server UTC time (DateTime.UtcNow), or exactly equal to current server UTC time
  - **When** POST /api/tasks is submitted
  - **Then** the request is rejected with 400 and details contains { field: "dueDate", issue: "must be future" }; no task is persisted

- [x] **AC-004.5: Client-supplied status is ignored**
  - **Given** a request body that omits status entirely, or includes a status field with any value (e.g. "Completed", "NotARealStatus")
  - **When** the task is created
  - **Then** the persisted and returned status is always "Pending" — CreateTaskRequest DTO has no status property so any client-supplied status value is silently dropped by model binding, never causes an error and never is honored

- [x] **AC-004.6: Multiple validation errors reported together**
  - **Given** a request with both an empty/whitespace title AND a past due date
  - **When** POST /api/tasks is submitted
  - **Then** the response is a single 400 with details[] containing entries for both fields (title and dueDate) — both violations are reported together, not just the first one encountered (CascadeMode.Continue behavior verified)

- [x] **AC-004.7: Response contract shape**
  - **Given** a successful task creation (any valid payload)
  - **When** the 201 response is returned
  - **Then** the response body contains exactly: id (uuid v7), title, description (nullable), status ("Pending"), dueDate (nullable, ISO 8601 if present), ownerId (uuid v7 matching seed constant), createdAt (ISO 8601 UTC), updatedAt (ISO 8601 UTC, equal to createdAt on creation)

- [x] **AC-004.8: Title max-length validation**
  - **Given** a request with title exceeding 200 characters (after trim)
  - **When** POST /api/tasks is submitted
  - **Then** the request is rejected with 400 and details contains { field: "title", issue: "title must not exceed 200 characters" }

- [x] **AC-004.9: Optional fields omitted**
  - **Given** a valid request omitting both description and dueDate (title-only payload)
  - **When** POST /api/tasks is submitted
  - **Then** the task is created successfully with 201; description and dueDate are null in the response; status is "Pending" and ownerId is set

- [x] **AC-004.10: Server-generated values cannot be overridden by client**
  - **Given** a request body containing extra/unknown fields (e.g. client-supplied id, ownerId, createdAt, updatedAt)
  - **When** POST /api/tasks is submitted
  - **Then** unknown/forbidden fields are silently ignored; server computes id (UUID v7), ownerId (from ICurrentUserContext), createdAt, and updatedAt — client-supplied values never override server-generated values

## Definition of Done

- [x] All ACs (AC-004.1 through AC-004.10) implemented and passing automated tests — no manual-only verification, no skipped or pending tests
- [x] Unit tests (Domain + Application) green: title empty/whitespace/max-length validation, description max-length validation, due-date past/boundary validation, status defaulting to Pending, use case orchestration with mocked ITaskRepository and ICurrentUserContext
- [x] Integration tests green against real PostgreSQL (Testcontainers): 201 happy path with full response contract shape, 400 empty title, 400 whitespace-only title, 400 title exceeding 200 chars, 400 past due date, 400 combined errors with multiple details entries, status ignored from body, optional fields null when omitted, ownerId matches seed constant
- [x] E2E tests green: task appears in UI list after creation, empty-title validation error shown in UI
- [x] API response for 201 matches exact contract shape from docs/architecture/api-contract.md section 4.1 — verified via integration test assertions deserializing into a strongly-typed DTO, not just status code checks
- [x] No regression in existing endpoints (GET /health) caused by Task entity or schema changes
- [x] Domain project (TaskFlow.Domain) has zero external PackageReferences — verified via .csproj review; Application references Domain abstractions only
- [x] CreateTaskRequest DTO does NOT declare a status property — compile-time contract enforcement, not runtime ignore
- [x] Error responses conform to { status, error, message, details: [{ field, issue }] } for all validation failure paths, with CascadeMode.Continue verified by multi-field-violation test
- [x] EF Core migration for Tasks table generated and applies cleanly against PostgreSQL (dotnet ef database update)
- [x] ownerId populated via ICurrentUserContext seed implementation, never hardcoded inline in use case or controller
- [x] Swagger/OpenAPI definition for POST /api/tasks updated to match implemented contract including max lengths and nullable fields
- [x] FE CreateTaskComponent implemented with reactive form, client-side validation mirroring BE rules, loading state on submit, field-level error mapping from API 400 responses, form reset on success
- [ ] Code reviewed and merged to feature/EP01-task-management with no unresolved PR comments

## Deliverables

- `src/TaskFlow.Domain/Entities/TaskItem.cs`
- `src/TaskFlow.Domain/Enums/TaskStatus.cs`
- `src/TaskFlow.Domain/Exceptions/DomainException.cs`
- `src/TaskFlow.Domain/Exceptions/InvalidTaskTitleException.cs`
- `src/TaskFlow.Domain/Exceptions/InvalidTaskDueDateException.cs`
- `src/TaskFlow.Domain/Constants/FieldLengths.cs`
- `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs`
- `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs`
- `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommand.cs`
- `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommandValidator.cs`
- `src/TaskFlow.Application/Tasks/Commands/CreateTask/CreateTaskCommandHandler.cs`
- `src/TaskFlow.Application/Tasks/Dtos/TaskDto.cs`
- `src/TaskFlow.Infrastructure/Persistence/Configurations/TaskItemConfiguration.cs`
- `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs`
- `src/TaskFlow.Infrastructure/Common/SeedCurrentUserContext.cs`
- `src/TaskFlow.Infrastructure/Persistence/Migrations/{timestamp}_AddTasksTable.cs`
- `src/TaskFlow.API/Controllers/TasksController.cs`
- `src/TaskFlow.API/Contracts/CreateTaskRequest.cs`
- `src/TaskFlow.API/Contracts/TaskResponse.cs`
- `src/TaskFlow.API/Middleware/ValidationExceptionHandler.cs`
- `web/src/app/features/tasks/create-task/create-task.component.ts`
- `web/src/app/features/tasks/create-task/create-task.component.html`
- `web/src/app/features/tasks/create-task/create-task.component.spec.ts`
- `web/src/app/features/tasks/data-access/task.service.ts`
- `web/src/app/features/tasks/data-access/task.service.spec.ts`
- `web/src/app/features/tasks/models/task.model.ts`
- `web/src/app/shared/utils/api-error-mapper.ts`
- `web/src/app/shared/utils/api-error-mapper.spec.ts`
- `tests/TaskFlow.Domain.Tests/Entities/TaskItemTests.cs`
- `tests/TaskFlow.Application.Tests/Tasks/Commands/CreateTask/CreateTaskCommandValidatorTests.cs`
- `tests/TaskFlow.Application.Tests/Tasks/Commands/CreateTask/CreateTaskCommandHandlerTests.cs`
- `tests/TaskFlow.IntegrationTests/Tasks/CreateTaskTests.cs`
- `e2e/src/tests/tasks/create-task.spec.ts`

## Test Plan

| Test Name | AC | Assertion |
|-----------|-----|-----------|
| Task_CreateWithEmptyTitle_ThrowsDomainException | AC-004.3 | TaskItem.Create(title: "", ...) throws InvalidTaskTitleException; Domain layer, zero mocks, pure entity logic |
| Task_CreateWithWhitespaceOnlyTitle_ThrowsDomainException | AC-004.3 | TaskItem.Create(title: "   ", ...) throws InvalidTaskTitleException after trim yields empty string |
| Task_CreateWithTitleExceedingMaxLength_ThrowsDomainException | AC-004.8 | TaskItem.Create(title: new string('a', 201), ...) throws InvalidTaskTitleException with message referencing 200-character limit |
| Task_CreateWithPastDueDate_ThrowsDomainException | AC-004.4 | TaskItem.Create(dueDate: DateTime.UtcNow.AddDays(-1), ...) throws InvalidTaskDueDateException |
| Task_CreateWithValidData_SetsStatusToPendingAndAssignsId | AC-004.1 | TaskItem.Create(...) returns entity with Status == Pending, Id is a non-empty version-7 Guid, Description == null and DueDate == null when not provided |
| CreateTaskCommandValidator_WithEmptyTitle_ReturnsValidationError | AC-004.3 | validator.TestValidate(command with Title="").ShouldHaveValidationErrorFor(x => x.Title) |
| CreateTaskCommandValidator_WithPastDueDate_ReturnsValidationError | AC-004.4 | validator.TestValidate(command with DueDate=past).ShouldHaveValidationErrorFor(x => x.DueDate) |
| CreateTaskCommandValidator_WithEmptyTitleAndPastDueDate_ReturnsBothErrors | AC-004.6 | validator.TestValidate(command with Title="", DueDate=past) returns result.Errors.Count == 2 confirming CascadeMode.Continue collects all failures |
| CreateTaskCommandValidator_WithValidCommand_NoValidationErrors | AC-004.1 | validator.TestValidate(valid command).ShouldNotHaveAnyValidationErrors() |
| CreateTaskCommandHandler_WithValidInput_ReturnsTaskDto | AC-004.1, AC-004.2 | Handler returns TaskDto with ownerId from ICurrentUserContext (NSubstitute mock), status Pending, and all submitted fields mapped; ITaskRepository.AddAsync called Received(1) |
| CreateTaskCommandHandler_AlwaysAssignsSeedOwnerId | AC-004.1, AC-004.7 | Returned DTO ownerId always equals the value from ICurrentUserContext, never null or user-supplied |
| CreateTask_WithEmptyTitle_Returns400 | AC-004.3 | Integration test: POST /api/tasks with title="" returns 400 with body matching error shape { status:400, error, message, details:[{field:"title", issue}] } |
| CreateTask_WithWhitespaceOnlyTitle_Returns400 | AC-004.3 | Integration test: POST /api/tasks with title="   " returns 400 with details[].field == "title" |
| CreateTask_WithTitleExceeding200Chars_Returns400 | AC-004.8 | Integration test: POST /api/tasks with 201-char title returns 400 with details containing field "title" and issue referencing 200-character limit |
| CreateTask_WithPastDueDate_Returns400 | AC-004.4 | Integration test: POST /api/tasks with dueDate in the past returns 400 with details[].field == "dueDate" |
| CreateTask_IgnoresStatusInBody_AlwaysDefaultsToPending | AC-004.5 | Integration test: POST /api/tasks with status:"Completed" in body still returns 201 with status:"Pending" in response |
| CreateTask_WithValidPayload_Returns201WithOwnerIdSet | AC-004.1, AC-004.2, AC-004.7 | Integration test: POST /api/tasks with title+description+future dueDate returns 201 with full response contract shape, non-empty UUID v7 ownerId matching seed constant, createdAt == updatedAt |
| CreateTask_WithTitleOnly_Returns201WithNullableFieldsNull | AC-004.9 | Integration test: POST /api/tasks with only title returns 201 with description:null and dueDate:null |
| CreateTask_WithMultipleInvalidFields_Returns400WithAllDetails | AC-004.6 | Integration test: POST /api/tasks with both empty title and past dueDate returns 400 with details[] containing two entries |
| CreateTask_WithClientSuppliedIdAndOwnerId_IgnoresClientValues | AC-004.10 | Integration test: POST /api/tasks with extra id/ownerId/createdAt fields in body returns 201; server-generated values differ from client-supplied values |
| CreateTask_FromUI_AppearsInTaskList | AC-004.1, AC-004.2 | E2E (Playwright): fill create-task form with title, submit, assert new task appears in task list with status Pending |
| CreateTask_WithEmptyTitle_ShowsValidationErrorInUI | AC-004.3 | E2E (Playwright): submit create-task form with empty title, assert inline validation error displayed, no navigation away from form |

## Validation Rules

- title: required, must not be null/empty/whitespace-only after trim (string.IsNullOrWhiteSpace), max 200 characters after trim
- description: optional (nullable), max 2000 characters if provided; empty string normalized to null at Application layer
- dueDate: optional (nullable), must be strictly greater than DateTime.UtcNow if provided (exclusive boundary — equal to "now" is rejected); ISO 8601 format
- status: NOT accepted in CreateTaskRequest DTO at all (no property declared) — always server-set to Pending at Domain entity construction
- id: server-generated UUID v7 via Guid.CreateVersion7() at Domain entity construction time — any client-supplied id is ignored
- ownerId: server-set from ICurrentUserContext (Delivery 1: SeedCurrentUserContext returns hardcoded UUID v7) — any client-supplied ownerId is ignored
- createdAt/updatedAt: server-set at entity construction time (DateTime.UtcNow), both equal on creation — any client-supplied values are ignored
- FluentValidation CascadeMode.Continue: all field validation errors collected and returned together in a single 400 response, never fail-fast on first rule
- Double validation: FluentValidation in Application layer validates command shape for API error response; Domain factory method (TaskItem.Create) independently re-asserts invariants as defense-in-depth

## Risks

- CRITICAL: Title max-length (200) and description max-length (2000) are team-proposed defaults — must be confirmed and documented in Domain constants BEFORE the EF Core migration is generated, since a post-migration schema change requires a follow-up migration and potential data handling
- CRITICAL: "Future" dueDate boundary condition (dueDate == now, exclusive) must be explicitly coded and tested at Domain, Application, and Integration layers identically — without a pinned-down rule, test authors will guess differently, producing flaky/inconsistent behavior near the boundary
- HIGH: Seed ownerId strategy is a temporary Delivery-1 shim via ICurrentUserContext — if not implemented as a single named constant (SeedOwnerId) referenced everywhere (API DI registration, test fixtures, seed migration), Delivery 3's JWT-claim cutover risks missing a hardcoded reference; flag SeedCurrentUserContext registration with TODO referencing Delivery 3 removal
- HIGH: ICurrentUserContext seed implementation risks being forgotten past Delivery 3 — must be flagged in Infrastructure DI registration with an explicit TODO and a Delivery 3 DOD item requiring its removal/replacement
- MEDIUM: EF Core ValueGeneratedOnAdd() behavior — if Guid.CreateVersion7() is called in Domain but EF Core's convention detects a Guid PK and applies ValueGeneratedOnAdd(), it may attempt to regenerate values on SaveChanges; must explicitly set ValueGeneratedNever() on Id in TaskItemConfiguration
- MEDIUM: Column naming convention (snake_case vs PascalCase) not yet confirmed for Npgsql — defaults to C# property names unless EFCore.NamingConventions package is added; must decide before first migration to avoid rewrite
- MEDIUM: Status enum representation (string vs smallint via HasConversion) impacts migration schema — must align with TL decision before first migration; changing HasConversion after data exists requires a follow-up migration
- MEDIUM: Testcontainers/PostgreSQL setup for IntegrationTests adds CI runtime and requires Docker availability — must be confirmed working in CI before this story is marked Done; this is the first story exercising the full EF Core + PostgreSQL stack, blocking all subsequent stories if infra is unstable
- LOW: E2E test CreateTask_FromUI_AppearsInTaskList has an implicit dependency on US-005 (List Tasks) rendering — if US-005 is not yet implemented, the E2E test cannot pass; must sequence US-004 before US-005 or stub list rendering independently
- LOW: dueDate timezone handling — if client sends ISO 8601 without explicit UTC offset (naive datetime), server behavior is ambiguous; recommend enforcing UTC normalization at API boundary

## Out of Scope

- Authentication/authorization middleware, JWT validation, and 401 responses — Delivery 3 / EP02
- Ownership-based 404 filtering (returning 404 when task belongs to another user) — Delivery 3; domain model carries ownerId from day 1 but enforcement is deferred
- User entity, registration, login endpoints — EP02 / Delivery 2
- Task status transition rules or state machine enforcement — status is free-form per project convention, no validation on allowed transitions in any delivery
- List/View/Update/Delete task endpoints — separate stories (US-005 through US-008)
- Pagination, filtering, or sorting of task lists — belongs to US-005/US-009
- Bulk/batch task creation or task templates
- Recurring tasks, due date reminders, or notifications
- Task assignment to users other than the owner (no multi-user collaboration)
- Rate limiting or request throttling on the create endpoint
- XSS sanitization/escaping at the API layer — UI rendering responsibility
- Internationalization/localization of validation error messages
- Performance/load testing of the create endpoint
- Offline support, request retry/backoff, or optimistic-then-rollback UI patterns beyond basic append-on-success

## Notes

- Description is optional
- Due date is optional
- Status is always "Pending" on creation (cannot be set by user)

## Related Documents

- [API Contract — Create Task](../architecture/api-contract.md#41-create-task--post-apitasks) — request/response shape and error codes
- [Testing Strategy — US-004 coverage](../architecture/testing-strategy.md#us-004--create-task-post-apitasks)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-005 — List Tasks](US-005-list-tasks.md) — where created tasks appear
