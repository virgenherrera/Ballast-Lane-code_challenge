# US-006 — View Task Detail

**Epic**: EP02 - Task Management
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **view the full details of a specific task** so that **I can see all its information**.

## Acceptance Criteria

- [ ] **AC-1: View own task**
  - **Given** an authenticated user requesting a task they own
  - **When** the request is processed
  - **Then** the system returns all task fields: title, description, status, due date

- [ ] **AC-2: Task not found**
  - **Given** an authenticated user requesting a task that does not exist
  - **When** the request is processed
  - **Then** the system returns a not-found error

- [ ] **AC-3: Access denied to other user's task**
  - **Given** an authenticated user requesting a task owned by another user
  - **When** the request is processed
  - **Then** the system returns a not-found error (not forbidden, to avoid leaking existence)

## Notes

- Return not-found (not forbidden) for tasks belonging to other users to prevent enumeration attacks
