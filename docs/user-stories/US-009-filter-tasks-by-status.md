# US-009 — Filter Tasks by Status

**Epic**: EP02 - Task Management
**Priority**: Should Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **filter my tasks by status** so that **I can focus on tasks in a specific state**.

## Acceptance Criteria

- [ ] **AC-1: Filter by single status**
  - **Given** an authenticated user with tasks in various statuses
  - **When** they request tasks filtered by "Pending"
  - **Then** only tasks with status "Pending" are returned

- [ ] **AC-2: Filter returns empty**
  - **Given** an authenticated user with no tasks matching the filter
  - **When** they apply a status filter
  - **Then** the system returns an empty list (not an error)

- [ ] **AC-3: Invalid filter value**
  - **Given** an authenticated user providing an invalid status value as filter
  - **When** the request is processed
  - **Then** the system rejects the request indicating valid status values

- [ ] **AC-4: No filter returns all**
  - **Given** an authenticated user requesting tasks without a filter
  - **When** the request is processed
  - **Then** all tasks are returned (same as US-005)

## Notes

- This can be implemented as a query parameter on the list endpoint (US-005)
- Only filters own tasks (ownership rule always applies)
