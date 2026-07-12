> [📚 INDEX](../../INDEX.md) / [EP02](../../epics/EP02-user-management.md) / EP02-B1-04

# Handoff: EP02-B1-04 — Application: AuthenticateUser Use Case

## 1. Metadata

| Field         | Value                                                        |
| ------------- | ------------------------------------------------------------------ |
| Task ID       | EP02-B1-04                                                           |
| Task Name     | Application: AuthenticateUser Use Case                               |
| Batch         | 1 of 6 (EP02)                                                        |
| Epic          | EP02 — User Management                                              |
| User Stories  | US-002 (AC-002.1 through AC-002.4)                                   |
| Persona       | Uncle Bob — Clean Architecture, DDD                                  |
| Model Tier    | sonnet                                                               |

## 2. Objective

Implement the `AuthenticateUser` use case: `AuthenticateUserCommand`,
`AuthenticateUserResult`, `AuthenticateUserHandler` (lookup → verify ALWAYS, even when
user is null, using a dummy hash → generate token), and `AuthenticateUserValidator`
(presence-only rules, no password-strength checks). Full unit test coverage of all 12
named test cases in US-002's Test Plan, proving the timing-attack mitigation
(Decision #9) and the identical-message guarantee across both failure paths.

## 3. Pre-Conditions

- [ ] `dotnet build src/TaskFlow.Application/` exits 0
- [ ] `dotnet build tests/TaskFlow.Application.Tests/` exits 0
- [ ] `src/TaskFlow.Domain/Entities/User.cs`, `ValueObjects/Email.cs`,
  `ValueObjects/PasswordHash.cs` exist (EP02-B1-01 DONE)
- [ ] `src/TaskFlow.Domain/Interfaces/IUserRepository.cs`,
  `Interfaces/IPasswordHasher.cs`, `Interfaces/ITokenService.cs`,
  `Exceptions/InvalidCredentialsException.cs` exist (EP02-B1-02 DONE)
- [ ] `TaskFlow.Application.csproj` references `FluentValidation 11.12.0` (already present)
- [ ] `TaskFlow.Application.Tests.csproj` references `NSubstitute 5.3.0` (already present)
- [ ] No folder `UseCases/AuthenticateUser/` exists under `src/TaskFlow.Application/`

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                       | Lines    | Why                                                             |
| -------------------------------------------------------------- | -------- | ---------------------------------------------------------------------- |
| `docs/user-stories/US-002-user-login.md`                          | 1-113    | Full DOR, DOD, AC-002.1 through AC-002.4, deliverables, validation rules |
| `docs/user-stories/US-002-user-login.md`                          | 78-89     | Test Plan — all 12 named test cases this task must implement             |
| `docs/epics/EP02-engineering-addenda.md`                            | 12-30    | Decision #2 — JWT claim set, expiry constant (900s)                       |
| `docs/epics/EP02-engineering-addenda.md`                            | 94-108   | Decision #9 — timing-attack prevention (constant-time comparison)         |
| `src/TaskFlow.Domain/Entities/User.cs`                              | all      | Entity shape for `AuthenticateUserResult`'s user summary                  |
| `src/TaskFlow.Domain/Interfaces/IUserRepository.cs`                 | all      | `GetByEmailAsync` signature to mock                                       |
| `src/TaskFlow.Domain/Interfaces/IPasswordHasher.cs`                 | all      | `Verify` signature to mock                                                |
| `src/TaskFlow.Domain/Interfaces/ITokenService.cs`                   | all      | `GenerateToken` signature to mock                                          |
| `src/TaskFlow.Domain/Exceptions/InvalidCredentialsException.cs`     | all      | `GenericMessage` constant to assert against                               |

## 5. Deliverables

### Files to Create

| File Path                                                                         | Contents                                                             |
| ---------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `src/TaskFlow.Application/UseCases/AuthenticateUser/AuthenticateUserCommand.cs`             | Input DTO: `Email`, `Password`                                                |
| `src/TaskFlow.Application/UseCases/AuthenticateUser/AuthenticateUserResult.cs`              | Output DTO: `AccessToken`, `TokenType`, `ExpiresIn`, user summary `{Id, Email, Name}` |
| `src/TaskFlow.Application/UseCases/AuthenticateUser/AuthenticateUserHandler.cs`             | validate → lookup → verify (ALWAYS) → generate token → result                 |
| `src/TaskFlow.Application/UseCases/AuthenticateUser/AuthenticateUserValidator.cs`           | Presence-only rules, no password-strength validation                          |
| `tests/TaskFlow.Application.Tests/UseCases/AuthenticateUser/AuthenticateUserHandlerTests.cs` | AC-002.1, AC-002.2, AC-002.4 — handler orchestration + timing-attack tests     |
| `tests/TaskFlow.Application.Tests/UseCases/AuthenticateUser/AuthenticateUserValidatorTests.cs` | AC-002.3 — presence-only validator tests                                    |

### Expected Signatures

```csharp
// UseCases/AuthenticateUser/AuthenticateUserCommand.cs
namespace TaskFlow.Application.UseCases.AuthenticateUser;

public sealed record AuthenticateUserCommand(string Email, string Password);

// UseCases/AuthenticateUser/AuthenticateUserResult.cs
namespace TaskFlow.Application.UseCases.AuthenticateUser;

public sealed record AuthenticateUserResult(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    AuthenticatedUserSummary User);

public sealed record AuthenticatedUserSummary(Guid Id, string Email, string Name);

// UseCases/AuthenticateUser/AuthenticateUserHandler.cs
namespace TaskFlow.Application.UseCases.AuthenticateUser;

using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Domain.ValueObjects;

public sealed class AuthenticateUserHandler
{
    // Named constant, single source of truth — do not hardcode 900 elsewhere.
    private const int ExpiresInSeconds = 900;

    // A syntactically-valid but never-matched hash, used ONLY to keep the Verify() call
    // shape identical when the user does not exist. This value never corresponds to any
    // real user's stored hash.
    private static readonly PasswordHash DummyHash = PasswordHash.Create(
        "$2a$12$CwTycUXWue0Thq9StjUM0uJ8Nlq/HJ/PXtL5DsAmxOM.MRp7z3Y0i");

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthenticateUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthenticateUserResult> Handle(AuthenticateUserCommand command, CancellationToken cancellationToken)
    {
        // 1. var email = Email.Create(command.Email);
        // 2. var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        // 3. var hashToVerifyAgainst = user?.PasswordHash ?? DummyHash;
        // 4. var isValid = _passwordHasher.Verify(command.Password, hashToVerifyAgainst);
        //    CRITICAL (AC-002.4 / Decision #9): this Verify call MUST execute on EVERY path,
        //    including when user is null. Do NOT short-circuit before this line.
        // 5. if (user is null || !isValid) throw new InvalidCredentialsException();
        //    Both failure branches throw the SAME exception type/message — do not
        //    differentiate wording between "user not found" and "wrong password".
        // 6. var token = _tokenService.GenerateToken(user);
        // 7. return new AuthenticateUserResult(
        //        token, "Bearer", ExpiresInSeconds,
        //        new AuthenticatedUserSummary(user.Id, user.Email.Value, user.Name));
    }
}

// UseCases/AuthenticateUser/AuthenticateUserValidator.cs
namespace TaskFlow.Application.UseCases.AuthenticateUser;

using FluentValidation;

public sealed class AuthenticateUserValidator : AbstractValidator<AuthenticateUserCommand>
{
    public AuthenticateUserValidator()
    {
        CascadeMode = CascadeMode.Continue;

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("EMAIL_REQUIRED");
            // Presence only — no format/casing/strength checks here. Login validation
            // must accept ANY non-empty string so weak/legacy passwords still authenticate.

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("PASSWORD_REQUIRED");
            // Presence only — deliberately NO MinimumLength, NO strength rules. A password
            // of "a" must pass THIS validator (strength was already enforced at registration
            // time; login must not re-reject legitimately weak/legacy credentials).
    }
}
```

**Required Test Names** (from US-002 Test Plan — implement ALL of these):

`AuthenticateUserHandlerTests.cs`:

1. `Handle_ValidCredentials_ReturnsTokenAndUserSummary` (AC-002.1)
2. `Handle_ValidCredentials_CallsVerifyWithCorrectArgs` (AC-002.1) — assert
   `Verify(command.Password, user.PasswordHash)` called with correct arguments
3. `Handle_UserNotFound_ThrowsInvalidCredentialsException` (AC-002.2) — `GetByEmailAsync`
   returns null → exception thrown, `GenerateToken` never called
4. `Handle_WrongPassword_ThrowsInvalidCredentialsException` (AC-002.2) — `Verify` returns
   false → same exception type, `GenerateToken` never called
5. `Handle_UserNotFoundVsWrongPassword_SameExceptionMessage` (AC-002.2) — assert both
   thrown exceptions' `.Message` are string-equal
6. `Handle_UserNotFound_StillCallsPasswordHasherVerify` (AC-002.4) — `Verify` invoked
   `Times.Once` even when user is null, using the dummy hash
7. `Handle_FailurePaths_NeverCallTokenService` (AC-002.2) — `GenerateToken` `Times.Never`
   on either failure path

`AuthenticateUserValidatorTests.cs`:

1. `Validate_EmptyEmail_FailsNamingEmailField` (AC-002.3)
2. `Validate_EmptyPassword_FailsNamingPasswordField` (AC-002.3)
3. `Validate_BothFieldsEmpty_ReturnsTwoDistinctErrors` (AC-002.3)
4. `Validate_WeakPassword_DoesNotFailStrengthRules` (AC-002.3) — password `"a"` passes
   login validation (no strength rules applied)
5. `InvalidCredentialsException_Message_NoFieldHints` (AC-002.2 — may already exist from
   EP02-B1-02; if so, do NOT duplicate, just confirm it passes)

## 6. Quality Gates

| #  | Gate                     | Command                                                                  | Pass Criteria         |
| -- | -------------------------- | -------------------------------------------------------------------------------- | ------------------------- |
| G1 | Compilation                | `dotnet build`                                                                    | exit 0                     |
| G2 | Application unit tests     | `dotnet test --filter "FullyQualifiedName~TaskFlow.Application.Tests"`            | exit 0, 0 failures, all 12 named tests present |
| G3 | Domain unit tests unbroken | `dotnet test --filter "FullyQualifiedName~TaskFlow.Domain.Tests"`                 | exit 0, 0 failures          |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Add EF Core, BCrypt.Net-Next, or any concrete JWT library package to
  `TaskFlow.Application.csproj` — only `FluentValidation` is allowed
- Implement `IUserRepository`, `IPasswordHasher`, or `ITokenService` concretely — mock
  all three with NSubstitute in tests; concrete implementations are Batch 2 scope
- Create `RegisterUser` use case files — that is EP02-B1-03's scope (parallel task,
  separate folder)
- Implement rate limiting or account lockout — that is Batch 4 (API middleware) scope
- Add a refresh-token flow — explicitly excluded per Decision #2

### SCOPE BOUNDARY — Stop when

- All 6 deliverable files exist and all 12 named tests pass
- All quality gates in Section 6 pass
- Do NOT proceed to EP02-B1-03 or Batch 2 work

## 8. Anti-Patterns

| Anti-Pattern                                                | Why It Fails                                                            | Do Instead                                                        |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------ | ------------------------------------------------------------------------ |
| Short-circuiting with `if (user is null) throw ...` BEFORE calling `Verify` | Skips the dummy-hash `Verify` call, reintroduces the timing-attack leak (AC-002.4) | Always call `_passwordHasher.Verify(...)` first, using `user?.PasswordHash ?? DummyHash`, THEN branch on the combined result |
| Writing two different exception messages for "not found" vs. "wrong password" | Violates AC-002.2's identical-message requirement, enables user enumeration | Throw the exact same `InvalidCredentialsException` (no custom message override) on both paths |
| Adding `MinimumLength(8)` or any strength rule to `AuthenticateUserValidator` | Login must accept legacy/weak passwords already stored — DOD explicitly forbids this | Presence-only: `NotEmpty()` on `Email` and `Password`, nothing else |
| Hardcoding `900` in multiple places instead of a named constant | DOD requires a single source of truth for `expiresIn`                        | Define `private const int ExpiresInSeconds = 900;` once, reference everywhere |
| Logging `command.Password` or the real/dummy `PasswordHash.Value` anywhere | Violates DOD — no plaintext password or hash in logs/exceptions/test output   | Never interpolate password/hash values into strings, logs, or assertion messages |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G3)
2. If G1 fails: confirm `User`, `Email`, `PasswordHash`, `IUserRepository`,
   `IPasswordHasher`, `ITokenService`, `InvalidCredentialsException` all exist from
   EP02-B1-01/EP02-B1-02; if missing, report BLOCKED rather than stubbing them here
3. If G2 fails on `Handle_UserNotFound_StillCallsPasswordHasherVerify`: re-check that
   `Verify` is called unconditionally before the null/bool branch — this is the most
   common mistake on this task
4. If G2 fails on `Handle_UserNotFoundVsWrongPassword_SameExceptionMessage`: confirm both
   throw sites use the parameterless `InvalidCredentialsException()` constructor with no
   custom message argument
5. If G3 (Domain tests) breaks: you likely modified a Domain file outside scope — revert
   any edit to `src/TaskFlow.Domain/` made during this task; this task is Application-only
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP
   and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in
   each attempt.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS

- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Unit tests cover Application use cases in isolation (mocked repos) — PRIMARY layer for this task
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT

- Respect the current phase — do not jump ahead
- Every decision must trace back to a requirement or acceptance criterion
- Version pinning: ALL dependencies use exact versions, never floating

### TASKFLOW-BUILD-PIPELINE

- PostgreSQL is the ONLY database engine — no InMemory/SQLite (not applicable here; use
  NSubstitute mocks exclusively for this task's tests)

## 11. Status Protocol

```text
Status: [DONE | FAILED | BLOCKED]
Progress: X/Y items (items = deliverables from Section 5)
Quality Gates: G1:PASS G2:PASS G3:PASS (or FAIL with error)
Blocker: (if BLOCKED — describe exactly what prevents progress)
Files Created: (list of new files)
Files Modified: (list of changed files)
```
