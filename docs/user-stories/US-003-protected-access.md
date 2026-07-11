# US-003 — Protected Access

**Epic**: EP01 - User Management
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
