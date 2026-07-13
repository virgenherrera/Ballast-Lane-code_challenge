> [📚 INDEX](../INDEX.md) / [EP04](../epics/EP04-frontend.md) / US-017

# US-017 — Task List View (Dashboard)

**Epic**: [EP04 - Frontend (Angular)](../epics/EP04-frontend.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **see a list of my tasks after logging in** so that **I can
quickly review what I need to do**.

## Acceptance Criteria

- [x] **AC-017.1: List displays core task fields**
  - **Given** an authenticated user with existing tasks
  - **When** the Task List page loads
  - **Then** the app calls `GET /api/tasks` and renders each task's title, status, and due date

- [x] **AC-017.2: Empty state message**
  - **Given** an authenticated user with no tasks
  - **When** the Task List page loads and `GET /api/tasks` returns an empty `items` array
  - **Then** the app displays an empty state message (e.g. "You have no tasks yet — create one to
    get started") instead of a blank list

- [x] **AC-017.3: Filter by status**
  - **Given** an authenticated user on the Task List page with tasks in multiple statuses
  - **When** they select a status from the filter dropdown (Pending, In Progress, Completed, or All)
  - **Then** the app calls `GET /api/tasks?status=<value>` and re-renders the list with only
    matching tasks

- [ ] **AC-017.4: Loading state**
  - **Given** the Task List page has just been navigated to, or a filter has just changed
  - **When** the `GET /api/tasks` request is in flight
  - **Then** the app displays a loading indicator and suppresses the empty-state message until the
    response resolves

- [x] **AC-017.5: Navigate to detail/edit**
  - **Given** an authenticated user viewing the task list
  - **When** they select a task row
  - **Then** the app navigates to the Task Detail/Edit page for that task's `id`

## Component Interaction

```mermaid
%% Task List page load and filter interaction
flowchart TD
    Enter([User navigates to dashboard]) --> Guard{Auth Guard: token valid?}
    Guard -->|No| Redirect[Redirect to Login]
    Guard -->|Yes| Loading[Show loading state]
    Loading --> Fetch["GET /api/tasks (+ status filter)"]
    Fetch -->|200 OK, items = []| Empty[Show empty state]
    Fetch -->|200 OK, items > 0| Render[Render task rows:\ntitle, status, dueDate]
    Fetch -->|401| Redirect

    Render --> FilterChange{User changes status filter?}
    FilterChange -->|Yes| Loading
    Render --> RowClick{User selects a task?}
    RowClick -->|Yes| Detail[Navigate to Task Detail/Edit]
```

## Related Documents

- [API Contract — List Tasks](../architecture/api-contract.md#42-list-tasks--get-apitasks) —
  request/response shape, filtering, and error codes
- [API Contract — Filtering](../architecture/api-contract.md#7-filtering) — status filter behavior
- [Testing Strategy — CRUD Coverage Through the Real UI](../architecture/testing-strategy.md#43-crud-coverage-through-the-real-ui)
- [EP04 — Frontend (Angular)](../epics/EP04-frontend.md)
- [US-018 — Task Form (Create/Edit)](US-018-task-form.md) — destination when creating or opening a
  task from this list
