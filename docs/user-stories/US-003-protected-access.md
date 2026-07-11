> [INDEX](../INDEX.md) / [EP02 — User Management](../epics/EP02-user-management.md) / US-003

# US-003 — Protected Access

**Epic**: [EP02 - User Management](../epics/EP02-user-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As an **authenticated user**, I want the system to **protect my resources** so that **only I can access my data**.

## Batch 1 Scope Note

US-003 is ONLY PARTIALLY deliverable in Batch 1 (Domain + Application). ACs 2-4 depend on
middleware (Batch 3) and GET /api/auth/me endpoint (Batch 3). Batch 1 delivers only the
Domain/Application **contracts** that US-003 will consume.

This story should be tracked as **"contract-only / partial"** for Batch 1. Full enforcement
and testing moves to Batch 3 (API layer) and Batch 5 (US-003 dedicated).

## Definition of Ready (DOR)

- [ ] Confirmed that US-003 is ONLY PARTIALLY deliverable in Batch 1. Batch 1 delivers Domain/Application contracts only.
- [ ] ITokenService interface shape decided: GenerateToken(User) -> string confirmed. ValidateToken(string) -> ClaimsPrincipal? RECOMMENDED for Batch 1 to avoid reopening Domain contracts in Batch 3.
- [ ] GET /api/auth/me is confirmed as the testing surface for US-003 within EP02 (Decision #6), but its handler is ABSENT from Batch 1 blueprint. Deferred to Batch 3.
- [ ] AC-003.4 (user isolation) has no business resource entity (Task) in EP02 to test against. Deferred to EP01.
- [ ] JWT claim names confirmed as literal keys: sub, email, name. .NET ClaimTypes remapping behavior documented (known gotcha: 'sub' remapped to long URI).
- [ ] Custom 401 shape (Decision #8) confirmed as API-layer concern, not Batch 1 unit test scope.
- [ ] Sprint board splits US-003 into "US-003a: Token contract (Batch 1)" and "US-003b: Enforcement + isolation (Batch 3+)".

## Definition of Done (DOD)

- [ ] Explicit documentation of which US-003 AC sub-parts are deliverable in Batch 1 vs. deferred. Story NOT marked 'done' — marked 'partial/contract-only'.
- [ ] ITokenService interface defined in Domain with GenerateToken(User) -> string at minimum. ValidateToken signature added if decided per DOR recommendation.
- [ ] User entity exposes properties needed for JWT claims (Id for sub, Email.Value for email, Name for name) without breaking encapsulation.
- [ ] No Domain or Application class references ASP.NET Core types (HttpContext, ClaimsPrincipal, [Authorize]).
- [ ] No test in Batch 1 claims to satisfy AC-003.2, AC-003.3, or AC-003.4. Explicitly 0% covered by design.
- [ ] A tracked follow-up item exists for full US-003 test coverage in Batch 3/5.

## Acceptance Criteria

- [ ] **AC-003.1: Valid token grants access** *(Batch 1: contract only)*
  - **Given** ITokenService contract is defined in Domain with GenerateToken(User) -> string
  - **When** a downstream batch implements token generation and validation
  - **Then** the interface exposes what a valid-token check needs. Batch 1 guarantees the CONTRACT exists and is consumed by AuthenticateUserHandler. Runtime token validation is NOT testable until Batch 2/3.

- [ ] **AC-003.2: Missing token denies access** *(DEFERRED to Batch 3)*
  - **Given** no HTTP pipeline exists in Batch 1
  - **When** a request without a token hits a protected endpoint
  - **Then** DEFERRED. Batch 1 contribution limited to ensuring exception types used by future middleware are defined in Domain.

- [ ] **AC-003.3: Expired or invalid token** *(DEFERRED to Batch 3)*
  - **Given** no JWT parsing/expiry logic lives in Domain or Application in Batch 1
  - **When** an expired or malformed token is presented
  - **Then** DEFERRED. Note: 'expired' and 'invalid signature' are two distinct failure modes needing distinct test cases when implemented.

- [ ] **AC-003.4: User isolation** *(DEFERRED to EP01)*
  - **Given** no user-owned business resource (Task entity) exists in EP02
  - **When** user isolation is tested
  - **Then** DEFERRED. Batch 1 ensures User.Id (UUID v7) is the sole authorization key for future resource scoping.

## Expected Deliverables — Batch 1

| File | Description |
| ---- | ----------- |
| `src/TaskFlow.Domain/Interfaces/ITokenService.cs` | GenerateToken(User) -> string. ValidateToken(string) -> ClaimsPrincipal? RECOMMENDED. |

No additional Batch 1 deliverables — ITokenService is the only new artifact. All other
US-003 artifacts are produced in Batch 3+ (middleware, controller, integration tests).

## Test Plan — Batch 1

| Test Name | AC | Assertion |
| --------- | -- | --------- |
| ITokenService_Contract_GenerateTokenAcceptsUserReturnsString | AC-003.1 | Interface compiles, mocked in AuthenticateUserHandler tests |
| User_Entity_ExposesClaimProperties | AC-003.1 | User has accessible Id (Guid), Email.Value (string), Name (string) |
| User_Create_GeneratesUniqueVersion7Guid | AC-003.4 | User.Id is non-empty, two creations produce distinct v7 GUIDs |

## Validation Rules

- No validation rules are implementable for US-003 in Batch 1. Token validation, 401 responses, and user isolation are Batch 3 concerns.
- ITokenService interface in Domain must NOT expose types from any JWT library (no JwtSecurityToken, no ClaimTypes). Only primitive and Domain types.
- User.Id (Guid, UUID v7) is the SOLE authorization key for future resource scoping.

## Out of Scope — Batch 1

- JWT parsing, signature verification, expiry checking — Batch 2 (Infrastructure)
- Authentication middleware / [Authorize] attribute — Batch 3 (API layer)
- GET /api/auth/me controller, route, and use case handler — Batch 3
- Custom 401 response shape (Decision #8) — Batch 3
- Cross-user data isolation testing — requires Task entity with ownership (EP01)
- Any GetCurrentUser/Me use case — not in Batch 1 blueprint
- JwtBearer configuration in Program.cs — Batch 3
- Clock-skew/leeway policy for token expiry — Batch 2+

## Notes

- All task endpoints are protected
- Only registration and login are public
- US-003 is cross-cutting: applies to every protected task endpoint

## Related Documents

- [API Contract — Tasks API (Protected)](../architecture/api-contract.md#4-tasks-api-protected) — where this story's rules are enforced
- [API Contract — GET /api/auth/me](../architecture/api-contract.md#33-current-user--get-apiauthme) — EP02-owned testing surface
- [Testing Strategy — US-003 coverage](../architecture/testing-strategy.md#us-003--protected-access-cross-cutting-all-apitasks)
- [EP02 Engineering Addenda](../epics/EP02-engineering-addenda.md) — binding engineering decisions
- [Clean Architecture — Cross-Cutting Concerns](../architecture/clean-architecture.md#6-cross-cutting-concerns)

This story is cross-cutting: it applies to every protected task endpoint. Related task user
stories: [US-004](US-004-create-task.md), [US-005](US-005-list-tasks.md),
[US-006](US-006-view-task-detail.md), [US-007](US-007-update-task.md),
[US-008](US-008-delete-task.md), [US-009](US-009-filter-tasks-by-status.md).
