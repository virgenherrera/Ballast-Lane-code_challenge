> [📚 INDEX](../INDEX.md) / [Architecture](../INDEX.md#architecture) / API Contract

# TaskFlow API Contract

Source of truth for every HTTP endpoint exposed by the TaskFlow ASP.NET Web API. This document
defines request/response shapes, authentication rules, status codes, and filtering behavior for
the two API groups: **Auth API** (public) and **Tasks API** (protected).

## Table of Contents

- [1. Endpoint Summary](#1-endpoint-summary)
- [2. Conventions](#2-conventions)
- [3. Auth API (Public)](#3-auth-api-public)
- [4. Tasks API (Protected)](#4-tasks-api-protected)
- [5. Authentication Flow](#5-authentication-flow)
- [6. Status Codes Reference](#6-status-codes-reference)
- [7. Filtering](#7-filtering)
- [8. Pagination](#8-pagination)

## 1. Endpoint Summary

| Method | Path | Auth | US | Description |
| ------ | ---- | ---- | -- | ----------- |
| GET | `/health` | Public | US-012 | Liveness + database connectivity check |
| POST | `/api/auth/register` | Public | US-001 | Register a new user account |
| POST | `/api/auth/login` | Public | US-002 | Authenticate and receive a JWT access token (rate-limited) |
| GET | `/api/auth/me` | Bearer | US-003 | Return the authenticated user's profile |
| POST | `/api/tasks` | Bearer | US-004 | Create a new task owned by the caller |
| GET | `/api/tasks` | Bearer | US-005, US-009 | List the caller's tasks with pagination, optionally filtered by status |
| GET | `/api/tasks/{id}` | Bearer | US-006 | Retrieve a single task owned by the caller |
| PATCH | `/api/tasks/{id}` | Bearer | US-007 | Partially update an existing task owned by the caller |
| DELETE | `/api/tasks/{id}` | Bearer | US-008 | Permanently delete a task owned by the caller |

Every protected endpoint additionally satisfies US-003 (protected access, token validation, and
user isolation) since that user story is a cross-cutting requirement, not a standalone endpoint.

### User Story Coverage

- [x] [US-001 — User Registration](../user-stories/US-001-user-registration.md) → `POST /api/auth/register`
- [x] [US-002 — User Login](../user-stories/US-002-user-login.md) → `POST /api/auth/login`
- [x] [US-003 — Protected Access](../user-stories/US-003-protected-access.md) → `GET /api/auth/me` + enforced on all `/api/tasks/*` endpoints via JWT middleware
- [x] [US-004 — Create Task](../user-stories/US-004-create-task.md) → `POST /api/tasks`
- [x] [US-005 — List Tasks](../user-stories/US-005-list-tasks.md) → `GET /api/tasks`
- [x] [US-006 — View Task Detail](../user-stories/US-006-view-task-detail.md) → `GET /api/tasks/{id}`
- [x] [US-007 — Update Task](../user-stories/US-007-update-task.md) → `PATCH /api/tasks/{id}`
- [x] [US-008 — Delete Task](../user-stories/US-008-delete-task.md) → `DELETE /api/tasks/{id}`
- [x] [US-009 — Filter Tasks by Status](../user-stories/US-009-filter-tasks-by-status.md) → `GET /api/tasks?status={status}`

## 2. Conventions

### 2.1 Base URL

All paths below are relative to the API base URL (e.g., `https://api.taskflow.example/`).

### 2.2 Authentication Header

Protected endpoints require a Bearer token on every request:

```text
Authorization: Bearer <jwt>
```

### 2.3 Standard Error Shape

Every error response across every endpoint uses this shape, regardless of status code:

```jsonc
{
  "status": "number (required, matches HTTP status code)",
  "error": "string (required, machine-readable error code, e.g. 'VALIDATION_ERROR')",
  "message": "string (required, human-readable summary)",
  "details": [
    {
      "field": "string (optional, present for field-level validation errors)",
      "issue": "string (required, description of what is wrong with this field)"
    }
  ]
}
```

- `details` is an empty array or omitted when the error is not field-specific (e.g., 401, 404).
- `details` is populated with one entry per invalid field when the error is `VALIDATION_ERROR`
  (e.g., missing required field, malformed email, weak password, invalid status value).

## Health Endpoint (Public)

### `GET /health`

Returns application liveness and database connectivity status. Used by Docker `HEALTHCHECK`
and monitoring.

**Response `200 OK`:**

```jsonc
{
  "status": "ok",            // always "ok" if the process is running
  "liveSince": "string",     // ISO 8601 timestamp of when the application started
  "db": "ok"                 // "ok" if PostgreSQL responds, "down" if unreachable
}
```

No authentication required. No request body. This endpoint is outside the `/api` prefix.

## 3. Auth API (Public)

### 3.1 Register — `POST /api/auth/register`

Maps to [US-001](../user-stories/US-001-user-registration.md) (AC-001.1 through AC-001.4).

**Auth**: None (public).

**Request Body**:

```jsonc
{
  "email": "string (required, valid email format, unique, lowercase only — uppercase rejected with 400)",
  "name": "string (required, non-empty)",
  "password": "string (required, min 8 chars, at least 1 uppercase, 1 number, 1 special character)"
}
```

**Success Response — `201 Created`**:

```jsonc
{
  "id": "string (uuid, required)",
  "email": "string (required)",
  "name": "string (required)",
  "createdAt": "string (ISO 8601 timestamp, required)"
}
```

**Error Responses**:

| Status | Condition | AC |
| ------ | --------- | -- |
| 400 | Missing required field (`email`, `name`, or `password`) | AC-001.4 |
| 400 | Email format invalid or contains uppercase characters | AC-001.1 |
| 400 | Password does not meet strength requirements (min 8, 1 upper, 1 number, 1 special) | AC-001.3 |
| 409 | Email already registered | AC-001.2 |

### 3.2 Login — `POST /api/auth/login`

Maps to [US-002](../user-stories/US-002-user-login.md) (AC-002.1 through AC-002.3).

**Auth**: None (public).

**Request Body**:

```jsonc
{
  "email": "string (required)",
  "password": "string (required)"
}
```

**Success Response — `200 OK`**:

```jsonc
{
  "accessToken": "string (JWT, required)",
  "tokenType": "string (constant 'Bearer', required)",
  "expiresIn": "number (seconds until expiry, required)",
  "user": {
    "id": "string (uuid, required)",
    "email": "string (required)",
    "name": "string (required)"
  }
}
```

**Error Responses**:

| Status | Condition | AC |
| ------ | --------- | -- |
| 400 | Missing `email` or `password` | AC-002.3 |
| 401 | Invalid email or password (generic message, does not reveal which field is wrong) | AC-002.2 |
| 429 | Too many login attempts — `Retry-After` header included (5 attempts/min/IP, exponential backoff) | Security |

Note on AC-002.2: the `message` field must read identically whether the email does not exist or the
password is wrong (e.g., `"Invalid email or password."`), to prevent user enumeration. Both code
paths must take the same time (dummy BCrypt.Verify when user not found) to prevent timing attacks.

### 3.3 Current User — `GET /api/auth/me`

Maps to [US-003](../user-stories/US-003-protected-access.md) (AC-003.1 through AC-003.3).

**Auth**: Bearer token (protected).

**Request Body**: None.

**Success Response — `200 OK`**:

```jsonc
{
  "id": "string (uuid v7, required)",
  "email": "string (required)",
  "name": "string (required)",
  "createdAt": "string (ISO 8601 timestamp, required)"
}
```

**Error Responses**:

| Status | Condition | AC |
| ------ | --------- | -- |
| 401 | Missing, invalid, or expired token | AC-003.2, AC-003.3 |

## 4. Tasks API (Protected)

All endpoints in this section require a valid `Authorization: Bearer <jwt>` header. This satisfies
[US-003](../user-stories/US-003-protected-access.md):

- AC-003.1: a valid token allows the request to proceed.
- AC-003.2: a missing token returns `401 Unauthorized`.
- AC-003.3: an expired or tampered token returns `401 Unauthorized`.
- AC-003.4: every query and mutation is scoped to the authenticated user's own tasks (enforced at the
  data-access layer via the `owner_id` claim extracted from the token).

### 4.1 Create Task — `POST /api/tasks`

Maps to [US-004](../user-stories/US-004-create-task.md) (AC-004.1 through AC-004.5).

**Request Body**:

```jsonc
{
  "title": "string (required, non-empty)",
  "description": "string (optional)",
  "dueDate": "string (ISO 8601 date, optional, must be in the future if provided)"
}
```

`status` is never accepted in the request body — it always defaults to `"Pending"` on creation
(AC-004.5).

**Success Response — `201 Created`**:

```jsonc
{
  "id": "string (uuid, required)",
  "title": "string (required)",
  "description": "string (nullable)",
  "status": "string (constant 'Pending', required)",
  "dueDate": "string (ISO 8601 date, nullable)",
  "ownerId": "string (uuid, required)",
  "createdAt": "string (ISO 8601 timestamp, required)",
  "updatedAt": "string (ISO 8601 timestamp, required)"
}
```

**Error Responses**:

| Status | Condition | AC |
| ------ | --------- | -- |
| 400 | `title` missing or empty | AC-004.3 |
| 400 | `dueDate` is in the past | AC-004.4 |
| 401 | Missing, invalid, or expired token | US-003 |

### 4.2 List Tasks — `GET /api/tasks`

Maps to [US-005](../user-stories/US-005-list-tasks.md) (AC-005.1 through AC-005.4) and
[US-009](../user-stories/US-009-filter-tasks-by-status.md) (AC-009.1 through AC-009.4).

**Query Parameters**:

| Parameter | Type | Required | Default | Description |
| --------- | ---- | -------- | ------- | ----------- |
| `status` | string, one of `Pending`, `In Progress`, `Completed` | No | — | Filter tasks to a single status. Omit to return all statuses (US-005 AC-005.1, US-009 AC-009.4) |
| `page` | integer (≥ 1) | No | `1` | Page number. Values ≤ 0 return `400`. Omit to get the first page |
| `perPage` | integer (1–MaxPerPage) | No | Server constant | Items per page. Default and max defined in `PaginationDefaults`. Values outside range return `400` |

See [Section 7 — Filtering](#7-filtering) for full behavior.
See [Section 8 — Pagination](#8-pagination) for paging mechanics.

**Success Response — `200 OK`**:

```jsonc
{
  "items": [
    {
      "id": "string (uuid, required)",
      "title": "string (required)",
      "status": "string (required, one of 'Pending' | 'In Progress' | 'Completed')",
      "dueDate": "string (ISO 8601 date, nullable)"
    }
  ],
  "paging": {
    "page": "number (required, current page number)",
    "perPage": "number (required, items per page)",
    "total": "number (required, total items matching the filter across all pages)",
    "prev": "string | null (required, relative URL to previous page, null on first page)",
    "next": "string | null (required, relative URL to next page, null on last page)"
  }
}
```

- `items` is `[]` (empty array) when the user has no tasks, none match the filter, or `page`
  exceeds total pages — never an error (US-005 AC-005.2, US-009 AC-009.2).
- `items` only ever contains tasks owned by the caller (US-005 AC-005.3).
- Each item exposes at minimum `title`, `status`, and `dueDate` as required by US-005 AC-005.4.
- `paging.prev` and `paging.next` preserve any active `status` filter in the URL.

**Error Responses**:

| Status | Condition | AC |
| ------ | --------- | -- |
| 400 | `status` query parameter has a value outside the valid enum | US-009 AC-009.3 |
| 400 | `page` ≤ 0 or `perPage` outside 1–100 | Pagination |
| 401 | Missing, invalid, or expired token | US-003 |

### 4.3 View Task Detail — `GET /api/tasks/{id}`

Maps to [US-006](../user-stories/US-006-view-task-detail.md) (AC-006.1 through AC-006.3).

**Path Parameters**:

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `id` | string (uuid) | Identifier of the task to retrieve |

**Success Response — `200 OK`**:

```jsonc
{
  "id": "string (uuid, required)",
  "title": "string (required)",
  "description": "string (nullable)",
  "status": "string (required, one of 'Pending' | 'In Progress' | 'Completed')",
  "dueDate": "string (ISO 8601 date, nullable)",
  "ownerId": "string (uuid, required)",
  "createdAt": "string (ISO 8601 timestamp, required)",
  "updatedAt": "string (ISO 8601 timestamp, required)"
}
```

**Error Responses**:

| Status | Condition | AC |
| ------ | --------- | -- |
| 401 | Missing, invalid, or expired token | US-003 |
| 404 | Task does not exist, **or** exists but is owned by another user | AC-006.2, AC-006.3 |

Note on AC-006.3: a task owned by another user returns `404 Not Found`, never `403 Forbidden`, so that
callers cannot distinguish "does not exist" from "not yours" — preventing enumeration attacks.

### 4.4 Update Task — `PATCH /api/tasks/{id}`

Maps to [US-007](../user-stories/US-007-update-task.md) (AC-007.1 through AC-007.6).

**Path Parameters**:

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `id` | string (uuid) | Identifier of the task to update |

**Request Body** (partial update — only send fields that changed):

```jsonc
{
  "title": "string (optional, non-empty if provided)",
  "description": "string (optional, nullable)",
  "status": "string (optional, one of 'Pending' | 'In Progress' | 'Completed')",
  "dueDate": "string (ISO 8601 date, optional, nullable, past dates allowed on update)"
}
```

**Success Response — `200 OK`**:

```jsonc
{
  "id": "string (uuid, required)",
  "title": "string (required)",
  "description": "string (nullable)",
  "status": "string (required, one of 'Pending' | 'In Progress' | 'Completed')",
  "dueDate": "string (ISO 8601 date, nullable)",
  "ownerId": "string (uuid, required)",
  "createdAt": "string (ISO 8601 timestamp, required)",
  "updatedAt": "string (ISO 8601 timestamp, required)"
}
```

**Error Responses**:

| Status | Condition | AC |
| ------ | --------- | -- |
| 400 | `title` provided as an empty string | AC-007.6 |
| 400 | `status` provided with a value outside the valid enum | AC-007.4 |
| 401 | Missing, invalid, or expired token | US-003 |
| 404 | Task does not exist, or is owned by another user | AC-007.5 |

### 4.5 Delete Task — `DELETE /api/tasks/{id}`

Maps to [US-008](../user-stories/US-008-delete-task.md) (AC-008.1 through AC-008.4).

**Path Parameters**:

| Parameter | Type | Description |
| --------- | ---- | ----------- |
| `id` | string (uuid) | Identifier of the task to delete |

**Success Response — `204 No Content`**: empty body.

**Error Responses**:

| Status | Condition | AC |
| ------ | --------- | -- |
| 401 | Missing, invalid, or expired token | US-003 |
| 404 | Task does not exist (including a task already deleted), or owned by another user | AC-008.2, AC-008.3, AC-008.4 |

Note on AC-008.4: deletion is a hard, permanent removal. Re-sending the same delete request after
success returns `404 Not Found`, not a server error, making the operation idempotent from the
caller's perspective.

## 5. Authentication Flow

The following sequence covers the full lifecycle: registration, login, use of a protected
endpoint with a valid token, and rejection of an invalid token.

```mermaid
%% Auth lifecycle: register -> login -> protected access -> token rejection
sequenceDiagram
    autonumber
    actor Client
    participant AuthAPI as Auth API
    participant TasksAPI as Tasks API
    participant DB as Database

    Client ->> AuthAPI: POST /api/auth/register
    AuthAPI ->> DB: INSERT user (hashed password)
    DB -->> AuthAPI: user record
    AuthAPI -->> Client: 201 Created (user profile)

    Client ->> AuthAPI: POST /api/auth/login
    AuthAPI ->> DB: SELECT user by email
    DB -->> AuthAPI: user record
    AuthAPI ->> AuthAPI: verify password hash, issue JWT
    AuthAPI -->> Client: 200 OK (accessToken)

    Client ->> TasksAPI: GET /api/tasks (Authorization: Bearer valid-jwt)
    TasksAPI ->> TasksAPI: validate signature, expiry, extract ownerId claim
    TasksAPI ->> DB: SELECT tasks WHERE owner_id = ownerId
    DB -->> TasksAPI: task rows
    TasksAPI -->> Client: 200 OK (task list)

    Client ->> TasksAPI: GET /api/tasks (Authorization: Bearer expired-or-tampered-jwt)
    TasksAPI ->> TasksAPI: validate signature/expiry fails
    TasksAPI -->> Client: 401 Unauthorized
```

## 6. Status Codes Reference

| Status | Name | Applies To |
| ------ | ---- | ---------- |
| 200 | OK | Successful `GET`, `PATCH`, `POST /api/auth/login` responses |
| 201 | Created | Successful `POST /api/auth/register`, `POST /api/tasks` |
| 204 | No Content | Successful `DELETE /api/tasks/{id}` |
| 400 | Bad Request | Validation errors: missing/empty required fields, weak password, invalid email format, uppercase email, past due date on create, invalid status enum value |
| 401 | Unauthorized | Missing `Authorization` header, invalid/expired/tampered JWT, or wrong login credentials |
| 404 | Not Found | Task does not exist, or exists but belongs to a different user (ownership violations are masked as not-found) |
| 409 | Conflict | Registration attempted with an email that already exists |
| 429 | Too Many Requests | Login rate limit exceeded (5 attempts/min/IP, exponential backoff) |

## 7. Filtering

The list endpoint (`GET /api/tasks`) supports optional status filtering via a single query
parameter, satisfying [US-009](../user-stories/US-009-filter-tasks-by-status.md).

| Aspect | Behavior |
| ------ | -------- |
| Parameter name | `status` |
| Location | Query string, e.g. `GET /api/tasks?status=Pending` |
| Accepted values | `Pending`, `In Progress`, `Completed` (must match the Task Status enum exactly) |
| Omitted parameter | Returns all of the caller's tasks regardless of status (US-005 behavior, US-009 AC-009.4) |
| No matches | Returns `200 OK` with `"items": []` — never an error (US-009 AC-009.2) |
| Invalid value | Returns `400 Bad Request` with a `VALIDATION_ERROR` naming the valid enum values (US-009 AC-009.3) |
| Scope | Filtering is always applied on top of the ownership rule — a user can never filter into another user's tasks (US-009 notes) |

**Example — valid filter**: `GET /api/tasks?status=In%20Progress` returns only the caller's tasks
currently `In Progress`.

**Example — invalid filter** (`GET /api/tasks?status=Archived`) returns:

```jsonc
{
  "status": 400,
  "error": "VALIDATION_ERROR",
  "message": "Invalid status filter value.",
  "details": [
    {
      "field": "status",
      "issue": "Must be one of: 'Pending', 'In Progress', 'Completed'."
    }
  ]
}
```

## 8. Pagination

The list endpoint (`GET /api/tasks`) supports cursor-free page-based pagination via `page` and
`perPage` query parameters. Pagination composes with the `status` filter — both are applied
simultaneously.

The endpoint works with **zero pagination parameters**. Calling `GET /api/tasks` without `page`
or `perPage` returns the first page using the server's default page size — both values are
defined as constants in the backend (`PaginationDefaults.Page`, `PaginationDefaults.PerPage`).
The client never needs to know or hardcode these defaults.

| Aspect | Behavior |
| ------ | -------- |
| Default page | `1` (first page) — from `PaginationDefaults.Page` |
| Default perPage | Server-defined constant (`PaginationDefaults.PerPage`) — the API contract does not hardcode this value; the backend owns it |
| Maximum perPage | Server-defined constant (`PaginationDefaults.MaxPerPage`) — values above return `400` |
| Minimum perPage | `1` — values below return `400` |
| Page ≤ 0 | Returns `400 Bad Request` |
| Page beyond total | Returns `200 OK` with `"items": []` and `paging.total` reflecting the real count |
| Ordering | By `createdAt` descending (newest first) — deterministic for paging stability |
| No params | Equivalent to `?page=1&perPage={PaginationDefaults.PerPage}` — always valid, always returns data if tasks exist |

**Example — first page**: `GET /api/tasks?page=1&perPage=5`

```jsonc
{
  "items": [ /* 5 tasks */ ],
  "paging": {
    "page": 1,
    "perPage": 5,
    "total": 12,
    "prev": null,
    "next": "/api/tasks?page=2&perPage=5"
  }
}
```

**Example — middle page with status filter**: `GET /api/tasks?status=Pending&page=2&perPage=5`

```jsonc
{
  "items": [ /* up to 5 pending tasks */ ],
  "paging": {
    "page": 2,
    "perPage": 5,
    "total": 8,
    "prev": "/api/tasks?status=Pending&page=1&perPage=5",
    "next": null
  }
}
```

**Example — page beyond total**: `GET /api/tasks?page=99&perPage=10`

```jsonc
{
  "items": [],
  "paging": {
    "page": 99,
    "perPage": 10,
    "total": 12,
    "prev": "/api/tasks?page=2&perPage=10",
    "next": null
  }
}
```

**Link format**: `prev` and `next` are relative paths (no host/scheme) including all active
query parameters. This makes them safe to use directly in the frontend without URL parsing.

**Implementation opacity**: the paging contract is engine-agnostic by design. The response
exposes `page`, `perPage`, and `total` — never database-specific artifacts like offsets,
cursors, row IDs, or query plans. The backend translates `page`/`perPage` into whatever the
data layer requires (e.g., `OFFSET`/`LIMIT` for SQL) internally. This keeps the API contract
stable regardless of the underlying database engine and prevents leaking infrastructure
details to consumers.

## Related Documents

- [Clean Architecture](clean-architecture.md) — how requests flow through layers to reach these endpoints
- [Tech Stack](tech-stack.md) — technology decisions behind this API (JWT, EF Core, PostgreSQL)
- [Testing Strategy — Mapping: Acceptance Criteria to Test Cases](testing-strategy.md#33-mapping-acceptance-criteria-to-test-cases) — integration tests for every endpoint here
- [EP02 — User Management](../epics/EP02-user-management.md) and [EP01 — Task Management](../epics/EP01-task-management.md) — epics these endpoints implement
