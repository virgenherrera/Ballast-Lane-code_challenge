> [📚 INDEX](../INDEX.md) / [EP01 — Task Management](../epics/EP01-task-management.md) / US-006

# US-006 — View Task Detail

**Epic**: [EP01 - Task Management](../epics/EP01-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **view the full details of a specific task** so that **I can see all its information**.

## Acceptance Criteria

- [ ] **AC-006.1: View own task**
  - **Given** an authenticated user requesting a task they own
  - **When** the request is processed
  - **Then** the system returns all task fields: title, description, status, due date

- [ ] **AC-006.2: Task not found**
  - **Given** an authenticated user requesting a task that does not exist
  - **When** the request is processed
  - **Then** the system returns a not-found error

- [ ] **AC-006.3: Access denied to other user's task**
  - **Given** an authenticated user requesting a task owned by another user
  - **When** the request is processed
  - **Then** the system returns a not-found error (not forbidden, to avoid leaking existence)

## Notes

- Return not-found (not forbidden) for tasks belonging to other users to prevent enumeration attacks

## Related Documents

- [API Contract — View Task Detail](../architecture/api-contract.md#43-view-task-detail--get-apitasksid) — request/response shape and error codes
- [Testing Strategy — US-006 coverage](../architecture/testing-strategy.md#us-006--view-task-detail-get-apitasksid)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-005 — List Tasks](US-005-list-tasks.md) — entry point that links to task detail
