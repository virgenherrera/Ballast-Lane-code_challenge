> [📚 INDEX](../INDEX.md) / [EP02 — Task Management](../epics/EP02-task-management.md) / US-007

# US-007 — Update Task

**Epic**: [EP02 - Task Management](../epics/EP02-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **update an existing task** so that **I can reflect changes in my work**.

## Acceptance Criteria

- [ ] **AC-007.1: Update title**
  - **Given** an authenticated user with an existing task
  - **When** they change the title and submit
  - **Then** the title is updated

- [ ] **AC-007.2: Update status**
  - **Given** an authenticated user with an existing task
  - **When** they change the status to a valid value
  - **Then** the status is updated

- [ ] **AC-007.3: Update description and due date**
  - **Given** an authenticated user with an existing task
  - **When** they change the description or due date
  - **Then** the fields are updated accordingly

- [ ] **AC-007.4: Invalid status transition**
  - **Given** an authenticated user providing an invalid status value
  - **When** they attempt to update
  - **Then** the system rejects the request indicating valid status values

- [ ] **AC-007.5: Cannot update another user's task**
  - **Given** an authenticated user attempting to update a task they do not own
  - **When** the request is processed
  - **Then** the system returns a not-found error

- [ ] **AC-007.6: Title cannot be empty**
  - **Given** an authenticated user updating a task with an empty title
  - **When** the request is processed
  - **Then** the system rejects the request

## Notes

- Partial updates should be supported (only send fields that changed)
- Due date validation: if provided, must be valid date (past dates allowed on update since a task might already be overdue)

## Related Documents

- [API Contract — Update Task](../architecture/api-contract.md#44-update-task--patch-apitasksid) — request/response shape and error codes
- [Testing Strategy — US-007 coverage](../architecture/testing-strategy.md#us-007--update-task-patch-apitasksid)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-006 — View Task Detail](US-006-view-task-detail.md) — where updated values are reflected
