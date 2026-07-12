# Handoff: EP02-B2-04 — JwtTokenService Implementation

> [📚 INDEX](../../INDEX.md) / [EP02 — User Management](../../epics/EP02-user-management.md) / EP02-B2-04

## 1. Metadata

| Field         | Value                                        |
| ------------- | --------------------------------------------- |
| Task ID       | EP02-B2-04                                     |
| Task Name     | JwtTokenService Implementation                |
| Batch         | 2 of 6 (EP02 Batches 1-6 Plan)                |
| Epic          | EP02 — User Management                        |
| User Stories  | US-002 (AC-002.1)                             |
| Persona       | Uncle Bob — Infrastructure / Security         |
| Model Tier    | sonnet                                         |

## 2. Objective

Implement `JwtTokenService`, the concrete `ITokenService` adapter that issues HS256-signed
JWTs with claims `sub` (User.Id), `email`, `name`, a 15-minute (900s) expiry, and
Issuer/Audience sourced from the existing `JwtOptions` configuration class. This unblocks
`EP02-B4-01` (login endpoint), which depends on a working concrete token issuer instead of
the `ITokenService` mock used in `EP02-B1-04`'s unit tests.

## 3. Pre-Conditions

- [ ] EP02-B1-02 reports DONE — `ITokenService` interface exists with signature
      `GenerateToken(User) -> string` (confirm exact return type — string JWT vs. a result
      DTO — via context bundle before implementing)
- [ ] `src/TaskFlow.Domain/Entities/User.cs` exists (from EP02-B1-01) exposing `Id`
      (Guid), `Email` (Email VO with `.Value`), `Name` (string)
- [ ] `src/TaskFlow.API/Configuration/JwtOptions.cs` exists with `Secret`, `Issuer`,
      `Audience` properties
- [ ] `dotnet build src/TaskFlow.Infrastructure/` exits 0
- [ ] `Microsoft.AspNetCore.Authentication.JwtBearer` resolvable on NuGet (pin exact
      version at implementation time — record the resolved version in the Status Protocol
      `NOTES` field and update `README.md`'s Version Manifest); note that only the
      `System.IdentityModel.Tokens.Jwt` token-writing APIs are needed for THIS task —
      the ASP.NET Core bearer-authentication middleware itself is out of scope (that is
      `EP02-B5-01`)
- [ ] No file named `JwtTokenService.cs` exists under `src/TaskFlow.Infrastructure/Security/`

If any pre-condition fails, report BLOCKED with the specific failing check.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                          | Lines   | Why                                                          |
| -------------------------------------------------------------- | ------- | --------------------------------------------------------------- |
| `docs/epics/EP02-engineering-addenda.md`                     | 21-34   | Decision #2 — HS256, 15min expiry, claim set, Issuer/Audience source |
| `docs/user-stories/US-002-user-login.md`                      | 13-24, 60-76 | JWT claim set frozen (DOR), `ITokenService` expected deliverable |
| `docs/architecture/api-contract.md`                           | 157-170 | Login success response shape — `expiresIn` field the caller reads from this service |
| `src/TaskFlow.API/Configuration/JwtOptions.cs`                | all     | Existing options class — Secret/Issuer/Audience already bound from env vars in `Program.cs` |
| `src/TaskFlow.API/Program.cs`                                 | 40-50   | Confirms `JwtOptions` is already registered via `AddOptions<JwtOptions>().Bind(...)` |
| `src/TaskFlow.Domain/Entities/User.cs`                        | all     | Entity whose claims populate the token (created in EP02-B1-01)  |

## 5. Deliverables

### Files to Create

| File Path                                                                | Contents                                                    |
| ---------------------------------------------------------------------------- | -------------------------------------------------------------- |
| `src/TaskFlow.Infrastructure/Security/JwtTokenService.cs`                   | Implements `ITokenService.GenerateToken`                     |
| `tests/TaskFlow.Infrastructure.Tests/Security/JwtTokenServiceTests.cs`      | Unit tests validating token structure, claims, and expiry     |

### Files to Modify

| File Path                                                     | Change                                                          |
| ------------------------------------------------------------------ | -------------------------------------------------------------------- |
| `src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj`      | Add `Microsoft.AspNetCore.Authentication.JwtBearer` PackageReference (provides `System.IdentityModel.Tokens.Jwt` transitively — confirm at implementation time whether a direct `System.IdentityModel.Tokens.Jwt` reference is needed instead; use whichever package actually exposes `JwtSecurityTokenHandler`/`SymmetricSecurityKey` after resolving the dependency tree) |
| `src/TaskFlow.API/Configuration/JwtOptions.cs`                    | Add `public int ExpirySeconds { get; set; } = 900;` — the 900s (15min) value is currently NOT represented in `JwtOptions`; add it as a constant-backed default so `EP02-B4-01`'s login handler and this service share one source of truth instead of hardcoding `900` in two places |

### Expected Signatures

```csharp
// JwtTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.API.Configuration; // adjust if JwtOptions moves to a shared location
using TaskFlow.Application.Common.Interfaces; // adjust to actual ITokenService namespace from EP02-B1-02
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim("name", user.Name),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_options.ExpirySeconds),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**NOTE**: If `ITokenService`'s frozen signature from `EP02-B1-02` returns something other
than a raw `string` (e.g., a `TokenResult` DTO bundling the token string with its expiry),
match the frozen interface exactly — this snippet illustrates the JWT-construction
mechanics, not the final method signature. `AuthenticateUserHandler` (EP02-B1-04) is
already written against the real interface; do not change it to fit this snippet.

**Required Test Names** (unit tests, zero real HTTP, zero database):

1. `GenerateToken_ValidUser_ReturnsWellFormedJwt` — asserts the returned string has 3
   dot-separated segments (header.payload.signature) and can be parsed by
   `JwtSecurityTokenHandler().ReadJwtToken(token)` without throwing
2. `GenerateToken_ValidUser_ContainsSubClaimMatchingUserId` — decode token, assert `sub`
   claim equals `user.Id.ToString()`
3. `GenerateToken_ValidUser_ContainsEmailClaimMatchingUserEmail` — assert `email` claim
   equals `user.Email.Value`
4. `GenerateToken_ValidUser_ContainsNameClaimMatchingUserName` — assert `name` claim
   equals `user.Name`
5. `GenerateToken_ValidUser_HasExpiryApproximately900SecondsFromNow` — assert
   `token.ValidTo` is within a tolerance window (e.g., ±5 seconds) of
   `DateTime.UtcNow.AddSeconds(900)` — do NOT assert exact equality, clock drift during
   test execution makes that flaky
6. `GenerateToken_ValidUser_SignedWithHmacSha256` — assert
   `token.Header.Alg == SecurityAlgorithms.HmacSha256`
7. `GenerateToken_ValidUser_IssuerAndAudienceMatchConfiguredOptions` — assert
   `token.Issuer` and the single audience in `token.Audiences` match the `JwtOptions`
   instance the service was constructed with

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
| -- | -------------------------- | -------------------------------------------------------------------------------------------------- | ---------------------- |
| G1 | Compilation | `dotnet build src/TaskFlow.Infrastructure/` | exit 0 |
| G2 | Unit tests | `dotnet test tests/TaskFlow.Infrastructure.Tests/ --filter "FullyQualifiedName~JwtTokenServiceTests"` | exit 0, all named tests passed |
| G3 | Expiry constant centralized | `grep -rn "900" src/TaskFlow.API/ src/TaskFlow.Infrastructure/` shows the literal `900` in exactly one place (`JwtOptions.ExpirySeconds` default) | verified by inspection |
| G4 | No hardcoded secret | `JwtTokenService.cs` contains no string literal used as a signing key — the secret must come only from `JwtOptions.Secret` (env-var sourced) | verified by inspection |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Implement JWT Bearer authentication middleware (validating incoming tokens on
  protected endpoints) — that is `EP02-B5-01`
- Implement `UserRepository` or `BcryptPasswordHasher` — those are `EP02-B2-02`/`EP02-B2-03`
- Wire DI registration (`AddScoped<ITokenService, JwtTokenService>`) in `Program.cs` —
  belongs to a later API batch
- Add a refresh-token mechanism — explicitly out of scope per Decision #2 ("No refresh
  token endpoint")
- Add any claim beyond `sub`, `email`, `name` — Decision #2 freezes the claim set; do not
  add `roles`, `iat`, or anything speculative
- Change `JwtOptions.Secret/Issuer/Audience` sourcing — they are already populated from
  `JWT_SECRET`/`JWT_ISSUER`/`JWT_AUDIENCE` env vars in `Program.cs`; only ADD
  `ExpirySeconds` to the class, do not touch the existing three properties' wiring

### SCOPE BOUNDARY — Stop when

- `JwtTokenService.cs` implements `ITokenService.GenerateToken` exactly, `JwtOptions` has
  the new `ExpirySeconds` property, and all named unit tests pass
- All quality gates in Section 6 pass
- Do NOT proceed to `EP02-B3-01` (register endpoint) or any other batch

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
| --------------------------------------------------------- | ------------------------------------------------------------------ | ------------------------------------------------ |
| Hardcoding `900` as a magic number inside `GenerateToken` AND separately inside a future login handler | Two sources of truth drift apart silently when one changes | Add `JwtOptions.ExpirySeconds = 900` as the single named constant-backed default; both this service and the future login handler read the same option |
| Using a `Claim("sub", ...)` string literal instead of `JwtRegisteredClaimNames.Sub` | Works, but diverges from the well-known claim-type constant used elsewhere and risks a typo | Use `System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames` constants for `sub`/`email` |
| Asserting exact `DateTime` equality on token expiry in tests | Test execution takes non-zero time; exact equality is flaky | Assert within a tolerance window (a few seconds) |
| Reading `JwtOptions.Secret` directly from `IConfiguration` inside `JwtTokenService` instead of via `IOptions<JwtOptions>` | Bypasses the Options pattern already established project-wide (see `Program.cs` comment: "Handlers must never read IConfiguration directly") | Inject `IOptions<JwtOptions>` |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G4)
2. If G1 fails: confirm which NuGet package actually exposes `JwtSecurityTokenHandler` in
   the resolved version tree — `Microsoft.AspNetCore.Authentication.JwtBearer` pulls in
   `System.IdentityModel.Tokens.Jwt` transitively in most versions, but if the build fails
   on missing types, add `System.IdentityModel.Tokens.Jwt` as a direct package reference
   instead
3. If G2 fails: for expiry-related test failures, widen the tolerance window slightly
   rather than asserting exact equality; for claim-mismatch failures, re-check
   `User.Email.Value` vs. `User.Email.ToString()` — use whichever accessor the VO exposes
4. If G3 fails: search for a second hardcoded `900` and replace it with a reference to
   `JwtOptions.ExpirySeconds`
5. If G4 fails: remove any hardcoded secret string and read exclusively from
   `JwtOptions.Secret`
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP
   and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in
   each attempt

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS

- ALL tests must pass before any commit
- New features require corresponding tests FIRST (TDD: Red/Green/Refactor)
- Breaking an existing test is a blocking issue
- Unit tests: Domain invariants + Application use cases (mocked repos)
- Integration tests: API level (AAA pattern), PRIMARY confidence layer
- E2E: Playwright, final quality gate
- Tests map directly to user story acceptance criteria

### TASKFLOW-ANTI-DRIFT

- Respect the current phase — do not jump ahead
- Every decision must trace back to a requirement or AC
- Version pinning: ALL dependencies use exact versions, never floating

### TASKFLOW-BUILD-PIPELINE

- Env vars are discrete (`JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`), validated at startup
  by `EnvVarValidator` — fail-fast with named error on missing vars
- All dependency versions pinned in README — Version Manifest, single source of truth

## 11. Status Protocol

Include this block EXACTLY in your final response. No variations.

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B2-04
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {MUST include: exact Microsoft.AspNetCore.Authentication.JwtBearer (or System.IdentityModel.Tokens.Jwt) version pinned, confirmation ExpirySeconds was added to JwtOptions as the single source of truth for 900}
```
