> [📚 INDEX](../INDEX.md) / [EP02 — Task Management](../epics/EP02-task-management.md) / US-009

# US-009 — Filter Tasks by Status

**Epic**: [EP02 - Task Management](../epics/EP02-task-management.md)
**Priority**: Should Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **filter my tasks by status** so that **I can focus on tasks in a specific state**.

## Acceptance Criteria

- [ ] **AC-009.1: Filter by single status**
  - **Given** an authenticated user with tasks in various statuses
  - **When** they request tasks filtered by "Pending"
  - **Then** only tasks with status "Pending" are returned

- [ ] **AC-009.2: Filter returns empty**
  - **Given** an authenticated user with no tasks matching the filter
  - **When** they apply a status filter
  - **Then** the system returns an empty list (not an error)

- [ ] **AC-009.3: Invalid filter value**
  - **Given** an authenticated user providing an invalid status value as filter
  - **When** the request is processed
  - **Then** the system rejects the request indicating valid status values

- [ ] **AC-009.4: No filter returns all**
  - **Given** an authenticated user requesting tasks without a filter
  - **When** the request is processed
  - **Then** all tasks are returned (same as US-005)

## Notes

- This can be implemented as a query parameter on the list endpoint (US-005)
- Only filters own tasks (ownership rule always applies)

## Related Documents

- [API Contract — Filtering](../architecture/api-contract.md#7-filtering) — query parameter behavior and error codes
- [Testing Strategy — US-009 coverage](../architecture/testing-strategy.md#us-009--filter-tasks-by-status-get-apitasksstatus)
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth requirement for this endpoint
- [US-005 — List Tasks](US-005-list-tasks.md) — base endpoint this story extends
