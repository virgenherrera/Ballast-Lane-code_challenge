# Handoff: EP01-B1-03b — Infrastructure: SeedCurrentUserContext (Delivery-1 Identity Shim)

## 1. Metadata

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | EP01-B1-03b                               |
| Task Name     | Infrastructure: SeedCurrentUserContext (Delivery-1 Identity Shim) |
| Batch         | 1 of N (EP01 Chunk-C)                     |
| Epic          | EP01 — Task Management                    |
| User Stories  | US-004 (AC-004.1, AC-004.7)              |
| Persona       | Kelsey Hightower — Infrastructure         |
| Model Tier    | sonnet                                    |

**Split rationale**: Separated from EP01-B1-03a per TL/Infra feedback. An identity shim is unrelated to EF Core persistence; burying it under a "Persistence" task title risks it being forgotten past the Delivery-3 JWT cutover.

## 2. Objective

Implement `ICurrentUserContext`'s Delivery-1 seed implementation (`SeedCurrentUserContext`) and define the single named `SeedOwnerId` constant that DI registration, the seed implementation, and ALL test fixtures reference. Mark with an explicit TODO for Delivery-3 removal.

## 3. Pre-Conditions

- [ ] EP01-B1-02 reports STATUS: DONE — `ICurrentUserContext` interface exists at `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs`
- [ ] `dotnet build src/TaskFlow.Application/` exits 0
- [ ] No existing `SeedOwnerId` constant defined anywhere in the solution (search `src/` for "SeedOwnerId" — must find zero matches)

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File                                                       | Lines     | Why                                          |
| ---------------------------------------------------------- | --------- | -------------------------------------------- |
| `docs/user-stories/US-004-create-task.md`                  | 13-27     | DOR — SeedOwnerId single-constant requirement, HIGH risk on Delivery-3 forgetting |
| `docs/user-stories/US-004-create-task.md`                  | 173-185   | Risks — HIGH: SeedOwnerId must be single named constant referenced everywhere |
| `src/TaskFlow.Application/Common/Interfaces/ICurrentUserContext.cs` | all | Interface being implemented                  |

## 5. Deliverables

### Files to Create

| File Path                                                         | Contents |
| ----------------------------------------------------------------- | -------- |
| `src/TaskFlow.Infrastructure/Identity/SeedIdentity.cs`            | Static class with `public static readonly Guid SeedOwnerId` — single source of truth |
| `src/TaskFlow.Infrastructure/Identity/SeedCurrentUserContext.cs`  | Sealed class implementing `ICurrentUserContext`, returns `SeedIdentity.SeedOwnerId` |

### Files to Modify

None. DI registration happens in EP01-B1-04a's Program.cs.

### Expected Signatures

```csharp
// SeedIdentity.cs
namespace TaskFlow.Infrastructure.Identity;
public static class SeedIdentity
{
    /// <summary>Single source of truth for the Delivery-1 seed owner.
    /// Referenced by DI registration, SeedCurrentUserContext, and all test fixtures.
    /// </summary>
    public static readonly Guid SeedOwnerId = Guid.Parse("01961234-5678-7abc-def0-123456789abc"); // Generate ONE UUID v7 literal, used everywhere
}

// SeedCurrentUserContext.cs
namespace TaskFlow.Infrastructure.Identity;
// TODO(Delivery-3): Replace with a JWT-claim-backed ICurrentUserContext implementation.
// This seed shim exists ONLY for Delivery 1 — remove/replace per Delivery-3 DOD.
public sealed class SeedCurrentUserContext : ICurrentUserContext
{
    public Guid OwnerId => SeedIdentity.SeedOwnerId;
}
```

**BINDING RULE**: `SeedIdentity.SeedOwnerId` is the ONLY location this UUID is ever defined. All test fixtures across Domain, Application, Integration, and E2E layers MUST reference this constant — never a re-typed literal.

## 6. Quality Gates

| #  | Gate                          | Command                                                                       | Pass Criteria         |
| -- | ----------------------------- | ----------------------------------------------------------------------------- | --------------------- |
| G1 | Compilation                   | `dotnet build src/TaskFlow.Infrastructure/`                                   | exit 0                |
| G2 | Single source of truth        | Count of `Guid.Parse` in `src/TaskFlow.Infrastructure/Identity/` = exactly 1  | verified              |
| G3 | TODO present                  | `SeedCurrentUserContext.cs` contains "TODO" and "Delivery-3"                  | verified              |
| G4 | No duplicate GUID literals    | Search entire `src/` for the exact GUID string literal — must appear in exactly 1 file | verified |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Register `SeedCurrentUserContext` in Program.cs DI — EP01-B1-04a owns composition root
- Introduce any JWT, ClaimsPrincipal, or HttpContext dependency — hardcoded Delivery-1 shim only
- Duplicate the SeedOwnerId GUID literal anywhere else — always reference `SeedIdentity.SeedOwnerId`
- Write tests for this task — the implementation is trivial (a single property returning a constant); its correctness is proven transitively by EP01-B1-04b's integration tests asserting `ownerId == SeedIdentity.SeedOwnerId`

### SCOPE BOUNDARY — Stop when:

- Both deliverable files exist and all quality gates pass
- Do NOT proceed to DI registration (EP01-B1-04a) or API work

## 8. Anti-Patterns

| Anti-Pattern                                    | Why It Fails                                            | Do Instead                               |
| ----------------------------------------------- | ------------------------------------------------------- | ---------------------------------------- |
| Hardcoding a different GUID in a test fixture   | Creates drift — tests assert against wrong owner        | Import `SeedIdentity.SeedOwnerId`        |
| Omitting the Delivery-3 TODO                    | Shim gets forgotten, ships past Delivery 3              | Explicit `// TODO(Delivery-3)` comment   |
| Placing SeedOwnerId in Domain or Application    | Domain has zero infra awareness; Application defines interfaces, not concrete values | Infrastructure is the correct home |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed
2. If G1 fails: check the `ICurrentUserContext` interface import path
3. If G2/G4 fails: search for and remove duplicate GUID literals; consolidate to `SeedIdentity.SeedOwnerId`
4. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

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

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B1-03b
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: 0
TESTS_FAILED: 0
NOTES: {the exact SeedOwnerId GUID literal chosen — downstream tasks reference this}
```
