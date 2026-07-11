# Handoff: EP01-B1-04a — API: Testcontainers + WebApplicationFactory Harness + Composition Root

## 1. Metadata

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | EP01-B1-04a                               |
| Task Name     | API: Testcontainers + WebApplicationFactory Harness + Composition Root |
| Batch         | 1 of N (EP01 Chunk-C)                     |
| Epic          | EP01 — Task Management                    |
| User Stories  | US-004 (enables all ACs via composition root wiring) |
| Persona       | Uncle Bob — Composition Root / Test Infrastructure |
| Model Tier    | sonnet                                    |

**Split rationale**: TL, QA, and QA-Auto independently flagged the original single API task (9+ tests + first-ever Testcontainers + DI wiring + controller + middleware) as oversized, conflating "is the harness reliable" with "does the feature pass." Split into 04a (harness + composition root, zero business logic) and 04b (controller + contracts + tests).

## 2. Objective

Stand up the reusable integration test infrastructure (`Testcontainers.PostgreSql` + `WebApplicationFactory<Program>`) and wire `Program.cs` as the composition root: `AddDbContext`, FluentValidation global registration with `CascadeMode.Continue`, and DI for `ITaskRepository`/`ICurrentUserContext`. Zero business-logic tests here — produces the harness EP01-B1-04b and all US-005 through US-008 will reuse.

## 3. Pre-Conditions

- [ ] EP01-B1-02, EP01-B1-03a, EP01-B1-03b all report STATUS: DONE
- [ ] `dotnet build` (full solution) exits 0
- [ ] Docker is running: `docker ps` succeeds
- [ ] EP01-B1-03a's migration applies cleanly against a fresh PostgreSQL instance
- [ ] Testcontainers.PostgreSql version pinned (same version used in EP01-B1-03a's tests)
- [ ] FluentValidation.DependencyInjectionExtensions 11.11.0 resolvable on NuGet

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File                                                       | Lines     | Why                                          |
| ---------------------------------------------------------- | --------- | -------------------------------------------- |
| `docs/user-stories/US-004-create-task.md`                  | 13-27     | DOR — CascadeMode.Continue as project-wide default |
| `docs/architecture/api-contract.md`                        | 64-83     | Standard error shape (section 2.3) — middleware will handle this |
| `src/TaskFlow.Infrastructure/Persistence/AppDbContext.cs`  | all       | DbContext to register in DI                  |
| `src/TaskFlow.Infrastructure/Identity/SeedCurrentUserContext.cs` | all | DI registration target                       |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | all | DI registration target |

## 5. Deliverables

### Files to Create

| File Path                                                                   | Contents |
| --------------------------------------------------------------------------- | -------- |
| `tests/TaskFlow.IntegrationTests/Common/TaskFlowWebApplicationFactory.cs`   | WebApplicationFactory<Program> subclass: spins up Testcontainers.PostgreSql (postgres:17.5), overrides DB connection string, runs migrations on startup |
| `tests/TaskFlow.IntegrationTests/Common/IntegrationTestBase.cs`             | Base class with IAsyncLifetime, exposes HttpClient, database reset between tests |
| `tests/TaskFlow.IntegrationTests/HarnessSmokeTests.cs`                      | 1 smoke test hitting GET /health to prove the harness works |

### Files to Modify

| File Path                                  | Change                                    |
| ------------------------------------------ | ----------------------------------------- |
| `src/TaskFlow.API/Program.cs`              | Composition root: AddDbContext(UseNpgsql), AddValidatorsFromAssembly, ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue, AddScoped<ITaskRepository, TaskRepository>, AddSingleton<ICurrentUserContext, SeedCurrentUserContext> |
| `src/TaskFlow.API/appsettings.Development.json` | Add local PostgreSQL connection string for dev (separate from Testcontainers) |
| `src/TaskFlow.API/TaskFlow.API.csproj`     | Add FluentValidation.DependencyInjectionExtensions 11.11.0 PackageReference |

### Expected Signatures

```csharp
// Program.cs composition root additions:
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue; // SET ONCE, GLOBALLY

builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskCommandValidator>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddSingleton<ICurrentUserContext, SeedCurrentUserContext>();

// TaskFlowWebApplicationFactory.cs:
public class TaskFlowWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17.5")
        .Build();
    // Override ConfigureWebHost to replace connection string
    // Apply migrations in InitializeAsync
}
```

## 6. Quality Gates

| #  | Gate                          | Command                                                                                          | Pass Criteria         |
| -- | ----------------------------- | ------------------------------------------------------------------------------------------------ | --------------------- |
| G1 | Compilation                   | `dotnet build`                                                                                   | exit 0                |
| G2 | Harness smoke test            | `dotnet test tests/TaskFlow.IntegrationTests/ --filter "FullyQualifiedName~HarnessSmokeTests"`   | exit 0, 1 passed (container starts, migration applies, health returns 200) |
| G3 | CascadeMode set globally      | Program.cs contains exactly 1 occurrence of `ValidatorOptions.Global.DefaultRuleLevelCascadeMode`| verified              |
| G4 | No regression on health       | Harness smoke test verifies GET /health returns 200                                              | verified via G2       |
| G5 | Previous tests still pass     | `dotnet test tests/TaskFlow.Domain.Tests/ && dotnet test tests/TaskFlow.Application.Tests/`      | exit 0                |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Write `TasksController`, `CreateTaskRequest`, `TaskResponse`, or `ValidationExceptionHandler` — EP01-B1-04b
- Write any CreateTask business-logic integration test — only a health-check smoke test proving the harness itself works
- Regenerate or touch Swagger/OpenAPI
- Add a second CascadeMode.Continue setting anywhere — one global point only
- Add authentication/authorization middleware — Delivery 3

### SCOPE BOUNDARY — Stop when:

- Harness starts Testcontainers PostgreSQL, migrations apply, Program.cs composition root is wired, smoke test passes
- All quality gates pass
- Do NOT write TasksController or any CreateTask-specific test

## 8. Anti-Patterns

| Anti-Pattern                                     | Why It Fails                                         | Do Instead                                |
| ------------------------------------------------ | ---------------------------------------------------- | ----------------------------------------- |
| Setting CascadeMode per-validator "just to be safe" | Duplicated config masks whether the global default works | Set exactly once in Program.cs         |
| Writing CreateTask tests here                    | Conflates harness reliability with feature correctness | Defer all CreateTask assertions to 04b    |
| Using `postgres:latest`                          | Non-reproducible across CI runs                       | Pin `postgres:17.5`                       |
| Registering ICurrentUserContext as Scoped        | SeedCurrentUserContext is stateless, no per-request state in Delivery 1 | Register as Singleton |

## 9. Rollback Guidance

1. Read the error output — identify which gate failed
2. If G2 (smoke test) fails and Docker/Testcontainers won't start: report BLOCKED (infra-availability), not FAILED
3. If G2 fails with a migration error: check the connection string override in the factory matches the container's generated connection string
4. If G3 fails: search Program.cs for a duplicate or missing `ValidatorOptions.Global` line
5. If G5 fails: the composition root change broke an existing test — revert and investigate
6. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report. Distinguish Docker/CI infra failure (BLOCKED) from code bug (FAILED).

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
TASK: EP01-B1-04a
FILES_CREATED: [list]
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {exact Testcontainers.PostgreSql version pinned, any DI registration decisions}
```
