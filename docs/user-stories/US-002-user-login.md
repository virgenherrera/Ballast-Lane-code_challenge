> [INDEX](../INDEX.md) / [EP02 — User Management](../epics/EP02-user-management.md) / US-002

# US-002 — User Login

**Epic**: [EP02 - User Management](../epics/EP02-user-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As a **registered user**, I want to **log in with my credentials** so that **I can access my tasks**.

## Definition of Ready (DOR)

- [ ] Generic error message text finalized and identical for both 'user not found' and 'wrong password' cases. Exact string approved by PO. Must be a named constant, not inline strings.
- [ ] AuthenticateUserResult DTO shape frozen: accessToken (string), tokenType ("Bearer"), expiresIn (900), user summary {id, email, name}.
- [ ] Timing-attack mitigation (Decision #7): AuthenticateUserHandler MUST call IPasswordHasher.Verify even when user not found, using a dummy/constant PasswordHash. Application-layer responsibility.
- [ ] JWT claim set frozen: sub (UUID v7), email, name. No additional claims in Batch 1.
- [ ] expiresIn source of truth: 900 seconds (15 min per Decision #2). Named constant, not hardcoded in multiple places.
- [ ] Rate limiting (5/min/IP) confirmed as Batch 3+/API middleware scope. Handler carries no rate-limit logic.
- [ ] Email casing at login: consistent with registration — reject uppercase per Decision #4.
- [ ] AuthenticateUserValidator checks presence only (email, password not empty). Does NOT validate password strength.
- [ ] IPasswordHasher.Verify(string, PasswordHash) -> bool signature frozen and compatible with both real-user and dummy-hash paths.

## Definition of Done (DOD)

- [ ] AC-002.1 through AC-002.4 verified with passing unit tests in TaskFlow.Application.Tests.
- [ ] AuthenticateUserHandler depends solely on IUserRepository, IPasswordHasher, ITokenService (Domain interfaces). Zero concrete references.
- [ ] Test exists proving IPasswordHasher.Verify is called even when GetByEmailAsync returns null (timing-attack mitigation). Verified via mock.Verify(Times.Once).
- [ ] Test exists proving both failure paths (user not found, wrong password) throw the SAME InvalidCredentialsException type with IDENTICAL message string. Cross-test assertion for exact equality.
- [ ] InvalidCredentialsException inherits DomainException, carries no HTTP status codes, message does not contain 'email', 'password', or 'user'.
- [ ] AuthenticateUserValidator validates presence only. Does NOT validate password strength for login. Dedicated test confirms weak password passes login validation.
- [ ] No test or code path allows distinguishing 'email not found' from 'wrong password' via exception message, type, or timing.
- [ ] ITokenService.GenerateToken is NEVER called on any failure path. Verified via mock.Verify(Times.Never).
- [ ] AuthenticateUserResult shape confirmed via unit test. No credential-derived field exposed.
- [ ] All tests use NSubstitute mocks. No real BCrypt or JWT.

## Acceptance Criteria

- [ ] **AC-002.1: Successful login**
  - **Given** a registered user submits their correct lowercase email and correct password
  - **When** AuthenticateUserHandler.Handle(AuthenticateUserCommand) executes
  - **Then** ITokenService.GenerateToken is called with the authenticated User. AuthenticateUserResult contains the token and user summary {id, email, name}. expiresIn derived from configured constant (900s).

- [ ] **AC-002.2: Invalid credentials**
  - **Given** a user submits a non-existent email OR an existing email with wrong password
  - **When** AuthenticateUserHandler executes
  - **Then** InvalidCredentialsException is thrown with IDENTICAL message in both cases. Handler invokes IPasswordHasher.Verify even when user not found (dummy hash, Decision #7). ITokenService.GenerateToken is never called.

- [ ] **AC-002.3: Required fields**
  - **Given** email or password is null, empty, or whitespace-only
  - **When** AuthenticateUserValidator validates the AuthenticateUserCommand
  - **Then** validation fails with details[] entries per missing field (PropertyName == 'Email' or 'Password'), BEFORE any repository or hasher call. Both fields reported if both missing.

- [ ] **AC-002.4: Timing-attack mitigation**
  - **Given** GetByEmailAsync returns null (email not found)
  - **When** AuthenticateUserHandler executes
  - **Then** IPasswordHasher.Verify is still invoked exactly once against a dummy/constant PasswordHash before throwing InvalidCredentialsException. Verifiable via mock.Verify(Times.Once).

## Expected Deliverables — Batch 1

### Domain Layer (TaskFlow.Domain)

| File | Description |
| ---- | ----------- |
| `Exceptions/InvalidCredentialsException.cs` | Inherits DomainException, generic message, no HTTP status codes |

### Application Layer (TaskFlow.Application)

| File | Description |
| ---- | ----------- |
| `UseCases/AuthenticateUser/AuthenticateUserCommand.cs` | Input DTO: Email, Password |
| `UseCases/AuthenticateUser/AuthenticateUserResult.cs` | Output DTO: AccessToken, TokenType, ExpiresIn, User summary |
| `UseCases/AuthenticateUser/AuthenticateUserHandler.cs` | validate -> lookup -> verify (ALWAYS) -> generate token -> result |
| `UseCases/AuthenticateUser/AuthenticateUserValidator.cs` | Presence-only rules, no password-strength validation |

### Unit Tests

| File | Covers |
| ---- | ------ |
| `tests/TaskFlow.Domain.Tests/Exceptions/InvalidCredentialsExceptionTests.cs` | AC-002.2 |
| `tests/TaskFlow.Application.Tests/UseCases/AuthenticateUser/AuthenticateUserHandlerTests.cs` | AC-002.1, AC-002.2, AC-002.4 |
| `tests/TaskFlow.Application.Tests/UseCases/AuthenticateUser/AuthenticateUserValidatorTests.cs` | AC-002.3 |

## Test Plan — Batch 1

| Test Name | AC | Assertion |
| --------- | -- | --------- |
| Handle_ValidCredentials_ReturnsTokenAndUserSummary | AC-002.1 | GenerateToken called once, result has AccessToken and user summary |
| Handle_ValidCredentials_CallsVerifyWithCorrectArgs | AC-002.1 | Verify called with (command.Password, user.PasswordHash) |
| Handle_UserNotFound_ThrowsInvalidCredentialsException | AC-002.2 | GetByEmailAsync null -> InvalidCredentialsException, GenerateToken never called |
| Handle_WrongPassword_ThrowsInvalidCredentialsException | AC-002.2 | Verify returns false -> same exception type, GenerateToken never called |
| Handle_UserNotFoundVsWrongPassword_SameExceptionMessage | AC-002.2 | Messages from both paths are string-equal |
| Handle_UserNotFound_StillCallsPasswordHasherVerify | AC-002.4 | Verify invoked Times.Once even when user null (dummy hash) |
| Handle_FailurePaths_NeverCallTokenService | AC-002.2 | GenerateToken Times.Never on either failure path |
| Validate_EmptyEmail_FailsNamingEmailField | AC-002.3 | PropertyName=='Email' |
| Validate_EmptyPassword_FailsNamingPasswordField | AC-002.3 | PropertyName=='Password' |
| Validate_BothFieldsEmpty_ReturnsTwoDistinctErrors | AC-002.3 | 2 distinct failures, no short-circuit |
| Validate_WeakPassword_DoesNotFailStrengthRules | AC-002.3 | Weak password 'a' passes login validation |
| InvalidCredentialsException_Message_NoFieldHints | AC-002.2 | Message does not contain 'email', 'password', or 'user' |

## Validation Rules

| Rule | Where Enforced |
| ---- | -------------- |
| Email presence (non-null, non-empty, non-whitespace) | Application FluentValidation (AuthenticateUserValidator) |
| Password presence (non-null, non-empty, non-whitespace) | Application FluentValidation (AuthenticateUserValidator) |
| Email casing at login: consistent with Decision #4 | Reject uppercase, same as registration |
| Generic error (identical message for both failure modes) | InvalidCredentialsException constant message |
| Timing-attack mitigation (Verify always called) | Application Handler level |

## Out of Scope — Batch 1

- Actual JWT signing/HS256 implementation — Batch 2 (Infrastructure)
- BCrypt.Verify concrete implementation — Batch 2 (Infrastructure)
- Rate limiting enforcement (5/min/IP) — Batch 3+ (API middleware)
- Refresh token flow — excluded per Decision #2
- HTTP 200/401/429 mapping, AuthController — Batch 3 (API layer)
- Account lockout after N failed attempts
- Session/device tracking or "remember me"
- Wall-clock timing-attack benchmark tests — Batch 2 follow-up

## Notes

- Error messages must not reveal whether the email exists (security)
- This is a public (non-authenticated) endpoint
- Token mechanism: JWT with HS256, 15min expiry (Decision #2)

## Related Documents

- [API Contract — Login](../architecture/api-contract.md#32-login--post-apiauthlogin) — request/response shape and error codes
- [Testing Strategy — US-002 coverage](../architecture/testing-strategy.md#us-002--user-login-post-apiauthlogin)
- [EP02 Engineering Addenda](../epics/EP02-engineering-addenda.md) — binding engineering decisions
- [US-001 — User Registration](US-001-user-registration.md) — prerequisite account creation
- [US-003 — Protected Access](US-003-protected-access.md) — what login unlocks
