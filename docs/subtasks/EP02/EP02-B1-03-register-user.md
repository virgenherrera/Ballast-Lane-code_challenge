> [📚 INDEX](../../INDEX.md) / [EP02](../../epics/EP02-user-management.md) / EP02-B1-03

# Handoff: EP02-B1-03 — Application: RegisterUser Use Case

## 1. Metadata

| Field         | Value                                                          |
| ------------- | ------------------------------------------------------------------ |
| Task ID       | EP02-B1-03                                                           |
| Task Name     | Application: RegisterUser Use Case                                  |
| Batch         | 1 of 6 (EP02)                                                        |
| Epic          | EP02 — User Management                                              |
| User Stories  | US-001 (AC-001.1 through AC-001.7)                                   |
| Persona       | Uncle Bob — Clean Architecture, DDD                                  |
| Model Tier    | sonnet                                                               |

## 2. Objective

Implement the `RegisterUser` use case: `RegisterUserCommand`, `RegisterUserResult`,
`RegisterUserHandler` (orchestrates validate → `ExistsAsync` → `Hash` → construct `User`
→ `AddAsync`), and `RegisterUserValidator` (FluentValidation, `CascadeMode.Continue`,
5 independent password rules + email + name). Full unit test coverage of all 25 named
test cases in US-001's Test Plan, using NSubstitute mocks only — no real database, no
real BCrypt.

## 3. Pre-Conditions

- [ ] `dotnet build src/TaskFlow.Application/` exits 0
- [ ] `dotnet build tests/TaskFlow.Application.Tests/` exits 0
- [ ] `src/TaskFlow.Domain/Entities/User.cs`, `ValueObjects/Email.cs`,
  `ValueObjects/PasswordHash.cs` exist (EP02-B1-01 DONE)
- [ ] `src/TaskFlow.Domain/Interfaces/IUserRepository.cs`,
  `Interfaces/IPasswordHasher.cs`, `Exceptions/DuplicateEmailException.cs` exist
  (EP02-B1-02 DONE)
- [ ] `TaskFlow.Application.csproj` references `FluentValidation 11.12.0` (already present)
- [ ] `TaskFlow.Application.Tests.csproj` references `NSubstitute 5.3.0` (already present)
- [ ] No folder `UseCases/RegisterUser/` exists under `src/TaskFlow.Application/`

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                       | Lines    | Why                                                       |
| -------------------------------------------------------------- | -------- | -------------------------------------------------------------- |
| `docs/user-stories/US-001-user-registration.md`                    | 1-135    | Full DOR, DOD, AC-001.1 through AC-001.7, deliverables, validation rules |
| `docs/user-stories/US-001-user-registration.md`                    | 96-121    | Test Plan — all 25 named test cases this task must implement    |
| `docs/epics/EP02-engineering-addenda.md`                            | 12-22    | Decision #1 — exact password policy values                       |
| `docs/epics/EP02-engineering-addenda.md`                            | 43-51    | Decision #4 — email casing/uniqueness rule                       |
| `docs/epics/EP02-engineering-addenda.md`                            | 53-61    | Decision #5 — UUID v7 strategy                                    |
| `src/TaskFlow.Domain/Entities/User.cs`                              | all      | `User.Create(Email, string, PasswordHash)` signature (from EP02-B1-01) |
| `src/TaskFlow.Domain/Interfaces/IUserRepository.cs`                 | all      | Method signatures to mock                                         |
| `src/TaskFlow.Domain/Interfaces/IPasswordHasher.cs`                 | all      | Method signatures to mock                                         |
| `src/TaskFlow.Domain/Exceptions/DuplicateEmailException.cs`         | all      | Exception to throw on duplicate                                   |

## 5. Deliverables

### Files to Create

| File Path                                                                    | Contents                                                       |
| --------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| `src/TaskFlow.Application/UseCases/RegisterUser/RegisterUserCommand.cs`             | Input DTO: `Email`, `Name`, `Password` (all `string`)                 |
| `src/TaskFlow.Application/UseCases/RegisterUser/RegisterUserResult.cs`              | Output DTO: `Id`, `Email`, `Name`, `CreatedAt` — no password/hash field |
| `src/TaskFlow.Application/UseCases/RegisterUser/RegisterUserHandler.cs`             | validate → `ExistsAsync` → `Hash` → construct `User` → `AddAsync`      |
| `src/TaskFlow.Application/UseCases/RegisterUser/RegisterUserValidator.cs`            | FluentValidation, `CascadeMode.Continue`, 5 password rules + email + name |
| `tests/TaskFlow.Application.Tests/UseCases/RegisterUser/RegisterUserHandlerTests.cs` | AC-001.1, AC-001.2 — handler orchestration tests                       |
| `tests/TaskFlow.Application.Tests/UseCases/RegisterUser/RegisterUserValidatorTests.cs` | AC-001.3, AC-001.4, AC-001.6, AC-001.7 — validator tests              |

### Expected Signatures

```csharp
// UseCases/RegisterUser/RegisterUserCommand.cs
namespace TaskFlow.Application.UseCases.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Name, string Password);

// UseCases/RegisterUser/RegisterUserResult.cs
namespace TaskFlow.Application.UseCases.RegisterUser;

public sealed record RegisterUserResult(Guid Id, string Email, string Name, DateTime CreatedAt);

// UseCases/RegisterUser/RegisterUserHandler.cs
namespace TaskFlow.Application.UseCases.RegisterUser;

using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Domain.ValueObjects;

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        // 1. var email = Email.Create(command.Email);
        //    NOTE: RegisterUserValidator (FluentValidation) runs BEFORE this handler in the
        //    real pipeline (Batch 3 wires it), but the handler still constructs the Email VO
        //    directly here as defense-in-depth — do not skip this even though the validator
        //    also checks casing/format.
        // 2. if (await _userRepository.ExistsAsync(email, cancellationToken)) throw new DuplicateEmailException();
        //    MUST throw BEFORE calling Hash or AddAsync (AC-001.2)
        // 3. var passwordHash = _passwordHasher.Hash(command.Password);
        // 4. var user = User.Create(email, command.Name, passwordHash);
        // 5. await _userRepository.AddAsync(user, cancellationToken);
        // 6. return new RegisterUserResult(user.Id, user.Email.Value, user.Name, user.CreatedAt);
    }
}

// UseCases/RegisterUser/RegisterUserValidator.cs
namespace TaskFlow.Application.UseCases.RegisterUser;

using FluentValidation;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        CascadeMode = CascadeMode.Continue;

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("EMAIL_REQUIRED")
            .Must(e => e == e?.ToLowerInvariant()).WithErrorCode("EMAIL_UPPERCASE")
            .EmailAddress().WithErrorCode("EMAIL_INVALID_FORMAT");
            // NOTE: exact regex/format rule must match Domain's Email VO checks —
            // FluentValidation's built-in EmailAddress() validator is the starting point;
            // adjust if it disagrees with the Domain VO's accepted format set.

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("NAME_REQUIRED")
            .MaximumLength(100).WithErrorCode("NAME_TOO_LONG");
            // NOTE: "whitespace-only after trim" must also fail — NotEmpty() alone does not
            // trim; consider .Must(n => !string.IsNullOrWhiteSpace(n)) explicitly.

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("PASSWORD_REQUIRED")
            .MinimumLength(8).WithErrorCode("PASSWORD_TOO_SHORT")
            .MaximumLength(72).WithErrorCode("PASSWORD_TOO_LONG")
            .Must(p => p.Any(char.IsUpper)).WithErrorCode("PASSWORD_MISSING_UPPERCASE")
            .Must(p => p.Any(char.IsDigit)).WithErrorCode("PASSWORD_MISSING_DIGIT")
            .Must(p => p.Any(c => !char.IsLetterOrDigit(c))).WithErrorCode("PASSWORD_MISSING_SPECIAL");
            // NOTE: each .Must() is registered as an INDEPENDENT RuleFor chain link — with
            // CascadeMode.Continue, FluentValidation still evaluates all of them and reports
            // one ValidationFailure per broken rule (AC-001.3 requires this, not fail-fast).
    }
}
```

**Required Test Names** (from US-001 Test Plan — implement ALL of these):

`RegisterUserHandlerTests.cs`:

1. `Handle_ValidCommand_CreatesUserAndReturnsResult` (AC-001.1)
2. `Handle_ValidCommand_HashesPasswordBeforePersisting` (AC-001.1)
3. `Handle_ValidCommand_GeneratesUuidV7ForNewUser` (AC-001.1)
4. `Handle_DuplicateEmail_ThrowsDuplicateEmailException` (AC-001.2)
5. `Handle_DuplicateEmail_DoesNotCallPasswordHasher` (AC-001.2)
6. `Handle_ValidName_TrimmedBeforeStorage` (AC-001.7)
7. `RegisterUserResult_HasNoPasswordOrHashField` (AC-001.1)
8. `Email_Equals_SameValue_ReturnsTrue` (AC-001.1 — may already exist from EP02-B1-01;
   if so, do NOT duplicate, just confirm it passes)
9. `PasswordHash_Constructor_NullOrEmpty_Throws` (AC-001.1 — may already exist from
   EP02-B1-01; if so, do NOT duplicate)

`RegisterUserValidatorTests.cs`:

1. `Validate_PasswordTooShort_FailsWithLengthError` (AC-001.3)
2. `Validate_PasswordExactlyMinLength_Passes` (AC-001.3)
3. `Validate_PasswordExceedsBcryptLimit_Fails` (AC-001.3)
4. `Validate_PasswordMissingUppercase_Fails` (AC-001.3)
5. `Validate_PasswordMissingDigit_Fails` (AC-001.3)
6. `Validate_PasswordMissingSpecialChar_Fails` (AC-001.3)
7. `Validate_PasswordMeetsAllRules_Passes` (AC-001.3)
8. `Validate_EmptyEmail_FailsNamingEmailField` (AC-001.4)
9. `Validate_EmptyName_FailsNamingNameField` (AC-001.4)
10. `Validate_EmptyPassword_FailsNamingPasswordField` (AC-001.4)
11. `Validate_AllFieldsMissing_ReturnsAllThreeErrors` (AC-001.4)
12. `Validate_UppercaseEmail_FailsValidation` (AC-001.6)
13. `Validate_NameWhitespaceOnly_FailsValidation` (AC-001.7)
14. `Validate_NameExceedsMaxLength_FailsValidation` (AC-001.7)

## 6. Quality Gates

| #  | Gate                    | Command                                                                    | Pass Criteria         |
| -- | ------------------------- | -------------------------------------------------------------------------------- | ------------------------- |
| G1 | Compilation               | `dotnet build`                                                                    | exit 0                     |
| G2 | Application unit tests    | `dotnet test --filter "FullyQualifiedName~TaskFlow.Application.Tests"`            | exit 0, 0 failures, all 23 named tests present |
| G3 | Domain unit tests unbroken | `dotnet test --filter "FullyQualifiedName~TaskFlow.Domain.Tests"`                 | exit 0, 0 failures          |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Add EF Core, BCrypt.Net-Next, or any concrete Infrastructure package to
  `TaskFlow.Application.csproj` — only `FluentValidation` is allowed
- Implement `IUserRepository` or `IPasswordHasher` concretely — mock both with
  NSubstitute in tests; concrete implementations are Batch 2 scope
- Create `AuthenticateUser` use case files — that is EP02-B1-04's scope (parallel task,
  separate folder)
- Wire `RegisterUserValidator` into ASP.NET Core's DI/pipeline or any controller — that
  is Batch 3 scope
- Auto-issue a token or auto-login on successful registration — explicitly out of scope
  per US-001 DOR/DOD

### SCOPE BOUNDARY — Stop when

- All 6 deliverable files exist and all 23 named tests pass
- All quality gates in Section 6 pass
- Do NOT proceed to EP02-B1-04 or Batch 2 work

## 8. Anti-Patterns

| Anti-Pattern                                              | Why It Fails                                                       | Do Instead                                                   |
| --------------------------------------------------------------- | ------------------------------------------------------------------------ | ------------------------------------------------------------------ |
| Using `CascadeMode.Stop` (fail-fast) on the Password rule chain | AC-001.3 requires ONE `ValidationFailure` per broken rule, not just the first | Keep `CascadeMode.Continue` at the validator level; each `.Must()` link independently evaluated |
| Calling `_passwordHasher.Hash` before `ExistsAsync` check        | Violates AC-001.2 — duplicate-email must short-circuit before hashing      | Always check `ExistsAsync` first, throw before any hashing               |
| Returning the raw `PasswordHash` or plaintext password in `RegisterUserResult` | Violates DOD — no credential-derived field exposed                | `RegisterUserResult` contains ONLY `{Id, Email, Name, CreatedAt}`         |
| Using a real BCrypt hasher or real EF repository in tests        | Slows tests, breaks unit-test isolation, violates TASKFLOW-TEST-HARNESS   | Use NSubstitute mocks: `Substitute.For<IUserRepository>()`, `Substitute.For<IPasswordHasher>()` |
| Logging `command.Password` anywhere, even in test failure messages | Violates DOD — no plaintext password in logs/exceptions/test output     | Never interpolate `command.Password` into any string, log, or assertion message |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G3)
2. If G1 fails: confirm `User`, `Email`, `PasswordHash`, `IUserRepository`,
   `IPasswordHasher`, `DuplicateEmailException` all exist from EP02-B1-01/EP02-B1-02; if
   missing, report BLOCKED rather than stubbing them here
3. If G2 fails on a specific named test: re-read the corresponding AC in Section 4's
   context bundle — the boundary conditions (exact 8-char min, 72-char max, exact-length
   edge cases) are the historically under-tested spots
4. If G3 (Domain tests) breaks: you likely modified a Domain file outside scope — revert
   any edit to `src/TaskFlow.Domain/` made during this task; this task is Application-only
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP
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
