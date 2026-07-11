> [📚 INDEX](../INDEX.md) / [EP01 — User Management](../epics/EP01-user-management.md) / US-003

# US-003 — Protected Access

**Epic**: [EP01 - User Management](../epics/EP01-user-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want the system to **protect my resources** so that **only I can access my data**.

## Acceptance Criteria

- [ ] **AC-1: Valid token grants access**
  - **Given** a user with a valid authentication token
  - **When** they request a protected endpoint
  - **Then** the system processes the request normally

- [ ] **AC-2: Missing token denies access**
  - **Given** a request without an authentication token
  - **When** it hits a protected endpoint
  - **Then** the system returns an unauthorized error

- [ ] **AC-3: Expired or invalid token**
  - **Given** a user with an expired or tampered token
  - **When** they request a protected endpoint
  - **Then** the system returns an unauthorized error

- [ ] **AC-4: User isolation**
  - **Given** an authenticated user
  - **When** they request resources
  - **Then** they can only see and modify their own data, never another user's

## Notes

- All task endpoints are protected
- Only registration and login are public

## Related Documents

- [API Contract — Tasks API (Protected)](../architecture/api-contract.md#4-tasks-api-protected) — where this story's rules are enforced
- [Testing Strategy — US-003 coverage](../architecture/testing-strategy.md#us-003--protected-access-cross-cutting-all-apitasks)
- [Clean Architecture — Cross-Cutting Concerns](../architecture/clean-architecture.md#6-cross-cutting-concerns)

This story is cross-cutting: it applies to every protected task endpoint. Related task user
stories: [US-004](US-004-create-task.md), [US-005](US-005-list-tasks.md),
[US-006](US-006-view-task-detail.md), [US-007](US-007-update-task.md),
[US-008](US-008-delete-task.md), [US-009](US-009-filter-tasks-by-status.md).
