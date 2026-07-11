> [INDEX](../INDEX.md) / [EP02](EP02-user-management.md) / Batch 1 Refinement

# EP02 — Batch 1 Refinement Summary

**Batch**: 1 — Domain + Application
**Scope**: User entity, Email/PasswordHash VOs, IUserRepository, ITokenService, IPasswordHasher, RegisterUserUseCase, AuthenticateUserUseCase, FluentValidation validators, unit tests
**Refined by**: Full scrum team (PO, SM, TL, FE, BA, QA, QA-Auto, Infra)

## Prerequisites

Before Batch 1 development starts, these items must be resolved:

- [ ] `global.json` created at repo root pinning exact .NET 10 SDK version (e.g. `10.0.301` with rollForward: `latestFeature`).
- [ ] FluentValidation NuGet package added to `src/TaskFlow.Application/TaskFlow.Application.csproj` with exact pinned version.
- [ ] NSubstitute NuGet package added to `tests/TaskFlow.Application.Tests/` and `tests/TaskFlow.Domain.Tests/` with exact pinned version.
- [ ] README Version Manifest updated with FluentValidation and NSubstitute exact versions.
- [ ] IPasswordHasher full interface frozen (Hash + Verify) before EITHER handler's tests are written.
- [ ] ITokenService interface shape decided: GenerateToken + ValidateToken or GenerateToken only.
- [ ] Email casing rule resolved as single source of truth: reject uppercase (Decision #4).
- [ ] Name field constraints decided: max 100 chars, trim whitespace, Unicode permitted.
- [ ] Password special-character set enumerated explicitly before validator tests are written.
- [ ] Generic error message string for InvalidCredentialsException finalized as named constant.
- [ ] AuthenticateUserResult DTO shape decided and frozen.
- [ ] FluentValidation CascadeMode.Continue confirmed as default for all validators.
- [ ] Domain purity rule: TaskFlow.Domain.csproj must have ZERO external PackageReferences.

## NuGet Packages Required

| Package | Version | Project |
| ------- | ------- | ------- |
| FluentValidation | 12.0.0 | src/TaskFlow.Application/TaskFlow.Application.csproj |
| NSubstitute | 5.3.0 | tests/TaskFlow.Application.Tests/TaskFlow.Application.Tests.csproj |
| NSubstitute | 5.3.0 | tests/TaskFlow.Domain.Tests/TaskFlow.Domain.Tests.csproj |

## TDD Order

Tests drive implementation. Create test files in this order:

1. `tests/TaskFlow.Domain.Tests/ValueObjects/EmailTests.cs` — smallest unit, no dependencies
2. `tests/TaskFlow.Domain.Tests/ValueObjects/PasswordHashTests.cs` — immutable wrapper, no dependencies
3. `tests/TaskFlow.Domain.Tests/Entities/UserTests.cs` — depends on Email + PasswordHash VOs
4. `tests/TaskFlow.Domain.Tests/Exceptions/DuplicateEmailExceptionTests.cs` — type/inheritance
5. `tests/TaskFlow.Domain.Tests/Exceptions/InvalidCredentialsExceptionTests.cs` — type/message
6. Domain interfaces (IUserRepository, IPasswordHasher, ITokenService) — no tests, but MUST be frozen before handler tests
7. `tests/TaskFlow.Application.Tests/UseCases/RegisterUser/RegisterUserValidatorTests.cs` — pure FluentValidation
8. `tests/TaskFlow.Application.Tests/UseCases/RegisterUser/RegisterUserHandlerTests.cs` — first NSubstitute mocks
9. `tests/TaskFlow.Application.Tests/UseCases/AuthenticateUser/AuthenticateUserValidatorTests.cs` — presence-only
10. `tests/TaskFlow.Application.Tests/UseCases/AuthenticateUser/AuthenticateUserHandlerTests.cs` — most complex mocks

## Risks

### CRITICAL

- **Email casing ambiguity**: Decision #4 says both "lowercase only" and "Backend 400 on uppercase". Must resolve to ONE behavior (recommend: reject) before writing Email VO.
- **ITokenService incomplete contract**: Batch 1 only defines GenerateToken. If Batch 3 needs ValidateToken, it forces reopening Domain. Recommend defining complete interface now.
- **US-003 scope mismatch**: US-003 listed as "Must Have" but Batch 1 file blueprint contains ZERO AC-003.2/003.3/003.4 artifacts. Split into US-003a (contract, Batch 1) and US-003b (enforcement, Batch 3).

### HIGH

- **"Confirmation received" ambiguity**: If PO expects email notification, that is undiscovered scope. Must confirm as "201 response body only".
- **BCrypt 72-byte truncation**: No max password length set. BCrypt silently ignores bytes beyond 72. Must add max-length 72 validation.
- **TOCTOU race condition**: Two concurrent registrations with same email can both pass ExistsAsync. Known gap, closable only with Batch 2 unique index.
- **Timing-attack mitigation**: Trivially violated if handler short-circuits with `if user == null throw` before Verify. AC-002.4 dummy-verify test is the ONLY guard.

### MEDIUM

- **AuthenticateUserResult shape undecided**: Most FE-consequential contract. If decided ad hoc, Batch 3 and EP04 build on unstable assumptions.
- **FluentValidation CascadeMode default**: Some versions default to Stop. If not configured to Continue, multi-field error aggregation fails.
- **Name field unconstrained**: No max length, no charset rules, no trim in original briefing.
- **NSubstitute missing**: Not in Application.Tests.csproj PackageReferences. Tests won't compile.
- **global.json missing**: With 31 SDKs installed, builds are non-deterministic.
- **No CI pipeline**: No automated build+test verification after commits.

### LOW

- **UUID v7 availability**: Depends on Guid.CreateVersion7() (.NET 9+). Target is net10.0, available.
- **15-minute JWT with no refresh**: FE session model = "silent logout after 15 min". Confirm intentional.
- **PasswordHash VO without format validation**: Allows arbitrary strings. Consider BCrypt format check in Batch 2.
- **EF Core mapping**: Value Objects must have constructor accessibility compatible with EF Core materialization.

## Related Documents

- [EP02 — User Management](EP02-user-management.md)
- [EP02 Engineering Addenda](EP02-engineering-addenda.md)
- [US-001 — User Registration](../user-stories/US-001-user-registration.md)
- [US-002 — User Login](../user-stories/US-002-user-login.md)
- [US-003 — Protected Access](../user-stories/US-003-protected-access.md)
