> [📚 INDEX](../INDEX.md) / [EP01 — User Management](../epics/EP01-user-management.md) / US-002

# US-002 — User Login

**Epic**: [EP01 - User Management](../epics/EP01-user-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As a **registered user**, I want to **log in with my credentials** so that **I can access my tasks**.

## Acceptance Criteria

- [ ] **AC-1: Successful login**
  - **Given** a registered user with valid credentials
  - **When** they submit their email and password
  - **Then** the system authenticates them and provides an access token/session

- [ ] **AC-2: Invalid credentials**
  - **Given** a user providing a wrong email or password
  - **When** they attempt to log in
  - **Then** the system rejects the request with a generic error (no hint about which field is wrong)

- [ ] **AC-3: Required fields**
  - **Given** a user submitting the login form with missing email or password
  - **When** the form is submitted
  - **Then** the system indicates the missing fields

## Notes

- Error messages must not reveal whether the email exists (security)
- This is a public (non-authenticated) endpoint
- Token/session mechanism to be defined during design phase

## Related Documents

- [API Contract — Login](../architecture/api-contract.md#32-login--post-apiauthlogin) — request/response shape and error codes
- [Testing Strategy — US-002 coverage](../architecture/testing-strategy.md#us-002--user-login-post-apiauthlogin)
- [US-001 — User Registration](US-001-user-registration.md) — prerequisite account creation
- [US-003 — Protected Access](US-003-protected-access.md) — what login unlocks
