> [📚 INDEX](../INDEX.md) / [EP04](../epics/EP04-frontend.md) / US-018

# US-018 — Task Form (Create/Edit)

**Epic**: [EP04 - Frontend (Angular)](../epics/EP04-frontend.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want to **create a new task or edit an existing one through a
form** so that **I can keep my task list accurate and up to date**.

## Acceptance Criteria

- [ ] **AC-018.1: Create a new task**
  - **Given** an authenticated user on the Task Form in "create" mode with a title, optional
    description, and optional due date filled in
  - **When** they submit the form
  - **Then** the app calls `POST /api/tasks` and, on success, navigates back to the Task List page
    with the new task visible

- [x] **AC-018.2: Edit an existing task, pre-populated**
  - **Given** an authenticated user opening the Task Form in "edit" mode for an existing task
  - **When** the page loads
  - **Then** the app calls `GET /api/tasks/{id}` and pre-fills the form fields (title, description,
    status, dueDate) with the returned values

- [x] **AC-018.3: Title validation**
  - **Given** an authenticated user on the Task Form (create or edit)
  - **When** they submit the form with an empty title
  - **Then** the form blocks submission client-side and shows a "title is required" message without
    calling the API

- [x] **AC-018.4: Status change on edit**
  - **Given** an authenticated user editing an existing task
  - **When** they change the status field (Pending, In Progress, Completed) and submit
  - **Then** the app calls `PATCH /api/tasks/{id}` with the updated status and the task list
    reflects the new status after navigating back

- [ ] **AC-018.5: Success feedback**
  - **Given** an authenticated user who successfully creates or updates a task
  - **When** the API responds with `201 Created` or `200 OK`
  - **Then** the app shows a success confirmation (toast or inline message) before or upon returning
    to the Task List page

## Component Interaction

```mermaid
%% Task Form shared between create and edit flows
flowchart TD
    EntryCreate([From Task List: "New Task"]) --> FormCreate[Task Form — create mode\nempty fields]
    EntryEdit([From Task List: select existing task]) --> FetchTask["GET /api/tasks/{id}"]
    FetchTask --> FormEdit[Task Form — edit mode\npre-populated fields]

    FormCreate --> Validate{Title present?}
    FormEdit --> Validate

    Validate -->|No| ShowError[Show "title is required"]
    ShowError --> FormCreate

    Validate -->|Yes, create mode| PostTask["POST /api/tasks"]
    Validate -->|Yes, edit mode| PatchTask["PATCH /api/tasks/{id}"]

    PostTask -->|201 Created| Success[Show success feedback]
    PatchTask -->|200 OK| Success
    Success --> BackToList[Navigate to Task List]

    PostTask -->|400| ShowError
    PatchTask -->|400 / 404| ShowError
```

## Related Documents

- [API Contract — Create Task](../architecture/api-contract.md#41-create-task--post-apitasks) —
  request/response shape and error codes for the create flow
- [API Contract — Update Task](../architecture/api-contract.md#44-update-task--patch-apitasksid) —
  request/response shape and error codes for the edit flow
- [Testing Strategy — CRUD Coverage Through the Real UI](../architecture/testing-strategy.md#43-crud-coverage-through-the-real-ui)
- [EP04 — Frontend (Angular)](../epics/EP04-frontend.md)
- [US-017 — Task List View (Dashboard)](US-017-task-list-view.md) — entry point into this form and
  destination after a successful submit
