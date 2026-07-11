> [📚 INDEX](../INDEX.md) / [EP01 — Task Management](../epics/EP01-task-management.md) / US-008

# US-008 — Delete Task

**Epic**: [EP01 - Task Management](../epics/EP01-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **delete a task** so that **I can remove tasks I no longer need**.

## Acceptance Criteria

- [ ] **AC-008.1: Successful deletion**
  - **Given** an authenticated user requesting to delete a task they own
  - **When** the request is processed
  - **Then** the task is permanently removed

- [ ] **AC-008.2: Task not found**
  - **Given** an authenticated user requesting to delete a task that does not exist
  - **When** the request is processed
  - **Then** the system returns a not-found error

- [ ] **AC-008.3: Cannot delete another user's task**
  - **Given** an authenticated user attempting to delete a task they do not own
  - **When** the request is processed
  - **Then** the system returns a not-found error

- [ ] **AC-008.4: Idempotent deletion**
  - **Given** a task that has already been deleted
  - **When** the same delete request is sent again
  - **Then** the system returns a not-found error (not a server error)

## Notes

- Hard delete (permanent removal), no soft delete needed for this scope

## Related Documents

- [API Contract — Delete Task](../architecture/api-contract.md#45-delete-task--delete-apitasksid) — request/response shape and error codes
- [Testing Strategy — US-008 coverage](../architecture/testing-strategy.md#us-008--delete-task-delete-apitasksid)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-005 — List Tasks](US-005-list-tasks.md) — where deletion is reflected
