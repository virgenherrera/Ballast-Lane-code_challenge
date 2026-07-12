# Handoff: EP02-B5-02 — GET /api/auth/me Endpoint

## 1. Metadata

| Field         | Value                                               |
| ------------- | --------------------------------------------------- |
| Task ID       | EP02-B5-02                                          |
| Task Name     | GET /api/auth/me Endpoint                           |
| Batch         | 5 of 6 (EP02 — Protected Access)                   |
| Epic          | EP02 — User Management                              |
| User Stories  | US-003 (AC-003.1), Decision #6                      |
| Persona       | Backend API Engineer                                |
| Model Tier    | sonnet                                              |

## 2. Objective

Add a `GET /api/auth/me` endpoint to `AuthController` that returns the authenticated user's profile (`id`, `email`, `name`, `createdAt`). This endpoint has `[Authorize]` at the action level (unlike `Register`/`Login` which remain public) and serves as the EP02-owned testing surface for US-003 (per Engineering Decision #6). The handler uses `ICurrentUserContext.OwnerId` to identify the caller, then queries `IUserRepository.GetByIdAsync` to retrieve the full user profile.

## 3. Pre-Conditions

- [ ] EP02-B5-01 complete (`dotnet build` exits 0 with JWT auth middleware registered)
- [ ] `src/TaskFlow.API/Controllers/AuthController.cs` exists with `Register` and `Login` actions
- [ ] `src/TaskFlow.Application/Common/Interfaces/IUserRepository.cs` exposes `GetByIdAsync(Guid id, CancellationToken ct)`
- [ ] `src/TaskFlow.Domain/Entities/User.cs` exists with `Id`, `Email`, `Name`, `CreatedAt` properties
- [ ] `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs` exposes `Guid OwnerId`

If any pre-condition fails, report BLOCKED immediately.

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File                                                              | Lines   | Why                                           |
| ----------------------------------------------------------------- | ------- | --------------------------------------------- |
| `src/TaskFlow.API/Controllers/AuthController.cs`                  | all     | Existing controller to add GetMe action       |
| `src/TaskFlow.Application/Common/Interfaces/IUserRepository.cs`   | all     | Repository interface — GetByIdAsync signature  |
| `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs` | all   | OwnerId property to use                       |
| `src/TaskFlow.Domain/Entities/User.cs`                            | all     | Entity properties for response mapping        |
| `docs/epics/EP02-engineering-addenda.md`                          | 69-77   | Decision #6 — GET /api/auth/me spec           |
| `src/TaskFlow.API/Program.cs`                                     | 85-105  | DI registrations — verify IUserRepository is registered |

## 5. Deliverables

### Files to Create

| File Path                                                        | Contents                                  |
| ---------------------------------------------------------------- | ----------------------------------------- |
| `src/TaskFlow.API/Contracts/Auth/MeResponse.cs`                  | Response record: `{ Id, Email, Name, CreatedAt }` |

### Files to Modify

| File Path                                          | Change                                                            |
| -------------------------------------------------- | ----------------------------------------------------------------- |
| `src/TaskFlow.API/Controllers/AuthController.cs`   | Add `ICurrentUserContext` + `IUserRepository` to constructor; add `GetMe` action with `[Authorize]` + `[HttpGet("me")]` |

### Expected Signatures

```csharp
// MeResponse.cs
namespace TaskFlow.API.Contracts.Auth;

public sealed record MeResponse(
    Guid Id,
    string Email,
    string Name,
    DateTime CreatedAt);
```

```csharp
// AuthController.cs — additions:
private readonly ICurrentUserContext _currentUserContext;
private readonly IUserRepository _userRepository;

// Constructor adds these two parameters alongside existing ones.

[Authorize]
[HttpGet("me")]
[ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetMe(CancellationToken ct)
{
    var userId = _currentUserContext.OwnerId;
    var user = await _userRepository.GetByIdAsync(userId, ct);

    if (user is null)
    {
        // Should not happen if JWT sub references a valid user, but defensive:
        return NotFound();
    }

    var response = new MeResponse(
        user.Id,
        user.Email.Value, // or user.Email depending on VO shape
        user.Name,
        user.CreatedAt);

    return Ok(response);
}
```

**Note on `user.Email`**: If `Email` is a value object with a `.Value` property, use `user.Email.Value`. If it's a plain string, use `user.Email`. Check the `User.cs` entity in the context bundle to determine which.

## 6. Quality Gates

| #  | Gate                    | Command                                                       | Pass Criteria                |
| -- | ----------------------- | ------------------------------------------------------------- | ---------------------------- |
| G1 | Compilation             | `dotnet build`                                                | exit 0                       |
| G2 | Unit tests              | `dotnet test tests/TaskFlow.Domain.Tests/`                    | exit 0, 0 failures           |
| G3 | Auth me tests           | `dotnet test --filter "FullyQualifiedName~GetMe"` (from B5-03) | Deferred to EP02-B5-03       |
| G4 | No public Register/Login broken | `dotnet test --filter "FullyQualifiedName~Register"` | exit 0, 0 failures (if register tests exist) |

Note: Integration tests for `GET /api/auth/me` (valid token 200, expired 401, no token 401, tampered 401) are delivered in EP02-B5-03. This task delivers the endpoint code only.

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Add `[Authorize]` at the `AuthController` class level — only the `GetMe` action gets `[Authorize]`; `Register` and `Login` MUST remain public
- Implement user profile update (PUT/PATCH) — only GET is in scope
- Add any new repository methods — use the existing `GetByIdAsync`
- Create a separate "profile" controller — this lives on `AuthController` per Decision #6
- Add pagination, filtering, or any query parameters to this endpoint
- Modify `ICurrentUserContext` interface — it already has everything needed (`Guid OwnerId`)

### SCOPE BOUNDARY — Stop when:

- `GET /api/auth/me` returns `{ id, email, name, createdAt }` for authenticated requests
- `GET /api/auth/me` returns 401 (standard error shape) for unauthenticated requests
- `dotnet build` exits 0
- Do NOT proceed to integration test creation (EP02-B5-03)

## 8. Anti-Patterns

| Anti-Pattern                                     | Why It Fails                                               | Do Instead                                          |
| ------------------------------------------------ | ---------------------------------------------------------- | --------------------------------------------------- |
| Adding `[Authorize]` to the entire `AuthController` | Breaks public Register/Login endpoints                   | Add `[Authorize]` only on the `GetMe` action        |
| Querying user by email instead of ID             | Unnecessary; `ICurrentUserContext.OwnerId` IS the user ID  | Use `IUserRepository.GetByIdAsync(ownerId)`         |
| Returning the password hash in the response      | Security violation                                         | Only map `Id`, `Email`, `Name`, `CreatedAt`         |
| Not propagating `CancellationToken`              | Blocks graceful shutdown; violates project convention       | Pass `ct` through to all async calls                |
| Creating a separate use case / handler class     | Overkill for a simple read-by-id; not every GET needs CQRS | Query repository directly in the controller action  |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed (G1-G4)
2. If G1 (compilation) fails: check `using` directives for `ICurrentUserContext` and `IUserRepository` namespaces; verify the constructor injection matches the DI registration in `Program.cs`
3. If G4 (existing register tests) fails: you likely broke the constructor signature — ensure backward compatibility by adding parameters at the end or using a pattern that doesn't break existing DI resolution
4. If `GetByIdAsync` doesn't exist on the repository interface: report BLOCKED — pre-condition #3 failed (Batch 1/2 incomplete)
5. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times after corrections, STOP and report FAILED with: (a) which gate, (b) full error output, (c) what was tried in each attempt.

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
- Version pinning: ALL dependencies use exact versions

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no InMemory/SQLite
- Docker Compose: postgres:17.5, taskflow-api, taskflow-web

### TASKFLOW-SECURITY
- JWT secret minimum 32 characters — validated at startup
- No plaintext passwords in logs, responses, or test output
- Standard error shape for all HTTP error responses (status, error, message, details)

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP02-B5-02
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {any issues or decisions made}
```
