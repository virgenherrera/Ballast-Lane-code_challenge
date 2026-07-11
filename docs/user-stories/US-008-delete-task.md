# US-008 — Delete Task

**Epic**: EP02 - Task Management
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **delete a task** so that **I can remove tasks I no longer need**.

## Acceptance Criteria

- [ ] **AC-1: Successful deletion**
  - **Given** an authenticated user requesting to delete a task they own
  - **When** the request is processed
  - **Then** the task is permanently removed

- [ ] **AC-2: Task not found**
  - **Given** an authenticated user requesting to delete a task that does not exist
  - **When** the request is processed
  - **Then** the system returns a not-found error

- [ ] **AC-3: Cannot delete another user's task**
  - **Given** an authenticated user attempting to delete a task they do not own
  - **When** the request is processed
  - **Then** the system returns a not-found error

- [ ] **AC-4: Idempotent deletion**
  - **Given** a task that has already been deleted
  - **When** the same delete request is sent again
  - **Then** the system returns a not-found error (not a server error)

## Notes

- Hard delete (permanent removal), no soft delete needed for this scope
