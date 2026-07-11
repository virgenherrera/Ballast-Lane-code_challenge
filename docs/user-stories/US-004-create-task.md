> [📚 INDEX](../INDEX.md) / [EP02 — Task Management](../epics/EP02-task-management.md) / US-004

# US-004 — Create Task

**Epic**: [EP02 - Task Management](../epics/EP02-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **create a new task** so that **I can track work I need to do**.

## Acceptance Criteria

- [ ] **AC-004.1: Successful task creation**
  - **Given** an authenticated user providing at least a title
  - **When** they submit the new task
  - **Then** the task is created with status "Pending" and associated to the user

- [ ] **AC-004.2: Full task creation**
  - **Given** an authenticated user providing title, description, and due date
  - **When** they submit the new task
  - **Then** all fields are saved correctly

- [ ] **AC-004.3: Title is required**
  - **Given** an authenticated user submitting a task without a title
  - **When** the form is submitted
  - **Then** the system rejects the request indicating the title is required

- [ ] **AC-004.4: Due date validation**
  - **Given** an authenticated user providing a due date in the past
  - **When** they submit the new task
  - **Then** the system rejects the request indicating the date must be in the future

- [ ] **AC-004.5: Default status**
  - **Given** an authenticated user creating a task without specifying a status
  - **When** the task is created
  - **Then** the status defaults to "Pending"

## Notes

- Description is optional
- Due date is optional
- Status is always "Pending" on creation (cannot be set by user)

## Related Documents

- [API Contract — Create Task](../architecture/api-contract.md#41-create-task--post-apitasks) — request/response shape and error codes
- [Testing Strategy — US-004 coverage](../architecture/testing-strategy.md#us-004--create-task-post-apitasks)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-005 — List Tasks](US-005-list-tasks.md) — where created tasks appear
