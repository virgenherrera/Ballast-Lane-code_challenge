> [📚 INDEX](../INDEX.md) / [EP01 — Task Management](../epics/EP01-task-management.md) / US-005

# US-005 — List Tasks

**Epic**: [EP01 - Task Management](../epics/EP01-task-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **see a list of all my tasks** so that **I can have an overview of my work**.

## Acceptance Criteria

- [ ] **AC-005.1: List own tasks**
  - **Given** an authenticated user with existing tasks
  - **When** they request their task list
  - **Then** the system returns all tasks belonging to that user

- [ ] **AC-005.2: Empty list**
  - **Given** an authenticated user with no tasks
  - **When** they request their task list
  - **Then** the system returns an empty list (not an error)

- [ ] **AC-005.3: Only own tasks**
  - **Given** multiple users with tasks in the system
  - **When** a user requests their task list
  - **Then** they only see their own tasks, never another user's

- [ ] **AC-005.4: Task summary information**
  - **Given** an authenticated user viewing the task list
  - **When** the list is displayed
  - **Then** each item shows at minimum: title, status, and due date

## Notes

- Ordering to be defined during design (default: most recent first, or by due date)
- Pagination is optional but welcome for large datasets

## Related Documents

- [API Contract — List Tasks](../architecture/api-contract.md#42-list-tasks--get-apitasks) — request/response shape and error codes
- [Testing Strategy — US-005 coverage](../architecture/testing-strategy.md#us-005--list-tasks-get-apitasks)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-009 — Filter Tasks by Status](US-009-filter-tasks-by-status.md) — extends this endpoint with filtering
