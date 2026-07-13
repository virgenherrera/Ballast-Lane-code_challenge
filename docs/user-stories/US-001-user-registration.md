> [INDEX](../INDEX.md) / [EP02 — User Management](../epics/EP02-user-management.md) / US-001

# US-001 — User Registration

**Epic**: [EP02 - User Management](../epics/EP02-user-management.md)
**Priority**: Must Have
**Status**: [ ] Not Started

## Story

As a **visitor**, I want to **register an account** so that **I can access the task management system**.

## Definition of Ready (DOR)

- [x] Password policy confirmed: min 8 chars, max 72 (BCrypt truncation limit), 1 uppercase, 1 digit, 1 special char. Exact special-character set enumerated and documented.
- [x] Email casing rule resolved: Email VO REJECTS uppercase at construction time (throws DomainException) per Decision #4. No silent normalization. RegisterUserValidator also enforces as defense-in-depth.
- [x] "Confirmation received" in AC-001.1 clarified: HTTP 201 response body only. No email/notification sending in Batch 1 scope.
- [x] Name field constraints defined: min 1 non-whitespace char after trim, max 100 chars, leading/trailing whitespace trimmed. Unicode permitted.
- [x] RegisterUserResult DTO shape frozen: {Id: Guid (UUID v7), Email: string, Name: string, CreatedAt: DateTime (ISO-8601)}.
- [x] 409 Conflict error message approved: generic wording that does not reveal whether conflict is exact or case-insensitive match.
- [x] Field-level error format confirmed against Standard Error Shape. PropertyName matches DTO property name exactly (Email, Name, Password).
- [x] UUID v7 generation strategy confirmed: Guid.CreateVersion7() available in .NET 10.
- [x] IUserRepository interface signatures frozen with CancellationToken on all async methods.
- [x] IPasswordHasher.Hash(string) -> PasswordHash confirmed as pure method with no side effects.
- [x] Handler orchestration order confirmed: validate -> ExistsAsync -> Hash -> construct entity -> AddAsync.
- [x] Duplicate-email check is advisory (TOCTOU gap acknowledged); final integrity via DB unique index in Batch 2.
- [x] Registration does NOT issue an access token (no auto-login).
- [x] FluentValidation CascadeMode.Continue confirmed for all validators.

## Definition of Done (DOD)

- [x] AC-001.1 through AC-001.7 verified with passing unit tests in TaskFlow.Application.Tests and TaskFlow.Domain.Tests.
- [x] User.cs compiles without any reference to Infrastructure, EF Core, BCrypt, or System.IdentityModel namespaces.
- [x] Email VO rejects uppercase input at construction (throws DomainException). Dedicated unit test confirms rejection.
- [x] Email VO rejects invalid formats at construction with domain exception, independent of FluentValidation.
- [x] PasswordHash VO is immutable, does not expose raw value via ToString().
- [x] RegisterUserHandler depends solely on IUserRepository, IPasswordHasher (Domain interfaces). Zero concrete references.
- [x] RegisterUserValidator implements 5 password rules (min 8, max 72, uppercase, digit, special) as independent rules with distinct ErrorCode.
- [x] RegisterUserValidator does NOT short-circuit: all field violations reported together.
- [x] DuplicateEmailException inherits DomainException, carries no HTTP status codes.
- [x] RegisterUserResult contains exactly {Id, Email, Name, CreatedAt}. No password or hash field.
- [x] Error responses identify EACH field independently. Field names match DTO properties exactly.
- [x] No plaintext password in any log, exception message, or test assertion output.
- [x] Code review confirms no Infrastructure or API namespace references in Domain/Application.
- [x] All tests use NSubstitute mocks. No real database or BCrypt.
- [x] Name: whitespace-only rejected, leading/trailing trimmed, >100 chars rejected.

## Acceptance Criteria

- [x] **AC-001.1: Successful registration**
  - **Given** a visitor submits a unique lowercase email, valid name (1-100 chars, trimmed), and password meeting all strength rules (8-72 chars, 1 uppercase, 1 digit, 1 special)
  - **When** RegisterUserHandler.Handle(RegisterUserCommand) executes
  - **Then** IUserRepository.AddAsync is called once with a User whose Id is UUID v7, PasswordHash != plain password, and RegisterUserResult is returned with {Id, Email, Name, CreatedAt}

- [x] **AC-001.2: Duplicate email rejection**
  - **Given** IUserRepository.ExistsAsync returns true for the given email
  - **When** RegisterUserHandler executes
  - **Then** DuplicateEmailException is thrown BEFORE IPasswordHasher.Hash or IUserRepository.AddAsync are invoked

- [x] **AC-001.3: Password strength validation**
  - **Given** a password that fails one or more strength rules (min 8, max 72, 1 uppercase, 1 digit, 1 special)
  - **When** RegisterUserValidator validates the RegisterUserCommand
  - **Then** validation fails with one ValidationFailure per broken rule, each with distinct PropertyName ('Password') and ErrorCode

- [x] **AC-001.4: Required field validation**
  - **Given** email, name, or password is null, empty, or whitespace-only
  - **When** RegisterUserValidator validates the RegisterUserCommand
  - **Then** each missing field produces its own ValidationFailure with correct PropertyName. All violations reported together, not fail-fast.

- [x] **AC-001.5: Email format validation**
  - **Given** an email with invalid format (missing @, missing domain, missing TLD, spaces in local part)
  - **When** the Email VO is constructed
  - **Then** a domain exception is thrown rejecting the invalid format (defense-in-depth, independent of FluentValidation)

- [x] **AC-001.6: Email uppercase rejection**
  - **Given** an email containing any uppercase characters
  - **When** the Email VO is constructed or RegisterUserValidator runs
  - **Then** the request is rejected per Decision #4 (reject, not normalize). Both Domain VO and Application validator enforce as defense-in-depth.

- [x] **AC-001.7: Name field validation**
  - **Given** a name that is only whitespace after trim, or exceeds 100 characters
  - **When** RegisterUserValidator validates the RegisterUserCommand
  - **Then** whitespace-only and over-length names are rejected. Valid names with surrounding whitespace are trimmed before persistence.

## Expected Deliverables — Batch 1

### Domain Layer (TaskFlow.Domain)

| File | Description |
| ---- | ----------- |
| `Entities/User.cs` | Entity with UUID v7, CreatedAt, Email VO, PasswordHash VO |
| `ValueObjects/Email.cs` | Immutable VO, rejects uppercase and invalid formats, value equality |
| `ValueObjects/PasswordHash.cs` | Immutable VO, rejects null/empty, hides raw value |
| `Interfaces/IUserRepository.cs` | GetByEmailAsync, GetByIdAsync, AddAsync, ExistsAsync (all with CancellationToken) |
| `Interfaces/IPasswordHasher.cs` | Hash(string) -> PasswordHash, Verify(string, PasswordHash) -> bool |
| `Exceptions/DomainException.cs` | Base exception, no HTTP status codes |
| `Exceptions/DuplicateEmailException.cs` | Inherits DomainException |

### Application Layer (TaskFlow.Application)

| File | Description |
| ---- | ----------- |
| `UseCases/RegisterUser/RegisterUserCommand.cs` | Input DTO: Email, Name, Password |
| `UseCases/RegisterUser/RegisterUserResult.cs` | Output DTO: Id, Email, Name, CreatedAt |
| `UseCases/RegisterUser/RegisterUserHandler.cs` | validate -> ExistsAsync -> Hash -> construct User -> AddAsync |
| `UseCases/RegisterUser/RegisterUserValidator.cs` | FluentValidation, independent rules, CascadeMode.Continue |

### Unit Tests

| File | Covers |
| ---- | ------ |
| `tests/TaskFlow.Domain.Tests/ValueObjects/EmailTests.cs` | AC-001.5, AC-001.6 |
| `tests/TaskFlow.Domain.Tests/ValueObjects/PasswordHashTests.cs` | AC-001.1 |
| `tests/TaskFlow.Domain.Tests/Entities/UserTests.cs` | AC-001.1 |
| `tests/TaskFlow.Domain.Tests/Exceptions/DuplicateEmailExceptionTests.cs` | AC-001.2 |
| `tests/TaskFlow.Application.Tests/UseCases/RegisterUser/RegisterUserHandlerTests.cs` | AC-001.1, AC-001.2 |
| `tests/TaskFlow.Application.Tests/UseCases/RegisterUser/RegisterUserValidatorTests.cs` | AC-001.3, AC-001.4, AC-001.6, AC-001.7 |

## Test Plan — Batch 1

| Test Name | AC | Assertion |
| --------- | -- | --------- |
| Handle_ValidCommand_CreatesUserAndReturnsResult | AC-001.1 | AddAsync called once, UUID v7, PasswordHash != raw, result has {Id, Email, Name, CreatedAt} |
| Handle_ValidCommand_HashesPasswordBeforePersisting | AC-001.1 | Hash called once, User has hasher output |
| Handle_ValidCommand_GeneratesUuidV7ForNewUser | AC-001.1 | Valid v7 Guid, two creations are distinct and time-ordered |
| Handle_DuplicateEmail_ThrowsDuplicateEmailException | AC-001.2 | ExistsAsync true -> DuplicateEmailException, AddAsync never called |
| Handle_DuplicateEmail_DoesNotCallPasswordHasher | AC-001.2 | ExistsAsync true -> Hash never invoked |
| Validate_PasswordTooShort_FailsWithLengthError | AC-001.3 | 7-char password fails min-length rule |
| Validate_PasswordExactlyMinLength_Passes | AC-001.3 | 8-char compliant password passes |
| Validate_PasswordExceedsBcryptLimit_Fails | AC-001.3 | >72 bytes rejected |
| Validate_PasswordMissingUppercase_Fails | AC-001.3 | Only uppercase rule fails |
| Validate_PasswordMissingDigit_Fails | AC-001.3 | Only digit rule fails |
| Validate_PasswordMissingSpecialChar_Fails | AC-001.3 | Only special-char rule fails |
| Validate_PasswordMeetsAllRules_Passes | AC-001.3 | Compliant password passes all rules |
| Validate_EmptyEmail_FailsNamingEmailField | AC-001.4 | PropertyName=='Email' |
| Validate_EmptyName_FailsNamingNameField | AC-001.4 | PropertyName=='Name' |
| Validate_EmptyPassword_FailsNamingPasswordField | AC-001.4 | PropertyName=='Password' |
| Validate_AllFieldsMissing_ReturnsAllThreeErrors | AC-001.4 | 3+ distinct failures, no short-circuit |
| Email_Constructor_InvalidFormat_ThrowsDomainException | AC-001.5 | Parametrized: 'foo', 'foo@', '@bar.com' all throw |
| Email_Constructor_ValidFormats_Succeeds | AC-001.5 | 'jane+tasks@example.com' constructs OK |
| Email_Constructor_UppercaseInput_ThrowsDomainException | AC-001.6 | Reject, not normalize |
| Validate_UppercaseEmail_FailsValidation | AC-001.6 | Defense-in-depth alongside VO |
| Validate_NameWhitespaceOnly_FailsValidation | AC-001.7 | '   ' rejected |
| Validate_NameExceedsMaxLength_FailsValidation | AC-001.7 | >100 chars rejected |
| Handle_ValidName_TrimmedBeforeStorage | AC-001.7 | '  Alice  ' -> 'Alice' |
| RegisterUserResult_HasNoPasswordOrHashField | AC-001.1 | Only {Id, Email, Name, CreatedAt} |
| Email_Equals_SameValue_ReturnsTrue | AC-001.1 | Value-object equality |
| PasswordHash_Constructor_NullOrEmpty_Throws | AC-001.1 | Guards against empty hash strings |

## Validation Rules

| Rule | Where Enforced |
| ---- | -------------- |
| Email format (simplified RFC 5322-subset) | Domain VO constructor + Application FluentValidation |
| Email casing (reject uppercase) | Domain VO constructor + Application FluentValidation |
| Email max length (254 chars) | Domain VO constructor |
| Name required (non-null, non-empty, non-whitespace) | Application FluentValidation |
| Name max length (100 chars) | Application FluentValidation |
| Name trimming (leading/trailing whitespace) | Handler or VO construction |
| Password min length (8 chars) | Application FluentValidation (independent rule) |
| Password max length (72 chars, BCrypt limit) | Application FluentValidation (independent rule) |
| Password uppercase (at least 1) | Application FluentValidation (independent rule) |
| Password digit (at least 1) | Application FluentValidation (independent rule) |
| Password special char (at least 1) | Application FluentValidation (independent rule) |
| PasswordHash VO (rejects null/empty) | Domain VO constructor |
| Duplicate email (advisory) | Handler via ExistsAsync (TOCTOU gap, DB index in Batch 2) |

## Out of Scope — Batch 1

- Email/welcome notification sending (no IEmailService)
- Auto-login / token issuance on registration
- EF Core persistence, migrations, unique index — Batch 2
- BCrypt concrete implementation — Batch 2
- HTTP 201/400/409 mapping, AuthController — Batch 3
- Rate limiting on registration endpoint
- Frontend rendering, Angular components, Zod schemas — EP04
- JSON serialization casing — Batch 3

## Notes

- Email format must be validated
- Password requirements: min 8, max 72, 1 uppercase, 1 digit, 1 special character
- This is a public (non-authenticated) endpoint

## Related Documents

- [API Contract — Register](../architecture/api-contract.md#31-register--post-apiauthregister) — request/response shape and error codes
- [Testing Strategy — US-001 coverage](../architecture/testing-strategy.md#us-001--user-registration-post-apiauthregister)
- [EP02 Engineering Addenda](../epics/EP02-engineering-addenda.md) — binding engineering decisions
- [US-002 — User Login](US-002-user-login.md) — next step after registration
- [US-003 — Protected Access](US-003-protected-access.md) — cross-cutting auth enforcement
