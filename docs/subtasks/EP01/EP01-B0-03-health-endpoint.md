# Handoff: EP01-B0-03 — Health Endpoint + Program.cs

## 1. Metadata

| Field | Value |
| --- | --- |
| Task ID | EP01-B0-03 |
| Task Name | Health Endpoint + Program.cs |
| Batch | 0 of 7 |
| Epic | EP01 — User Management |
| User Stories | none (infrastructure — health endpoint defined in API Contract, not a user story) |
| Persona | Uncle Bob — Clean Architecture Author |
| Model Tier | sonnet |

## 2. Objective

Turn `Program.cs` into the composition root: register ASP.NET's built-in health checks, expose
`GET /health` returning liveness + database status, wire `appsettings` from env vars, and
fail fast at startup if a required env var is missing. This is the first observable proof that
the API boots and can (attempt to) reach PostgreSQL.

## 3. Pre-Conditions

- [ ] `EP01-B0-01` (Solution Scaffold) reports `DONE` — `TaskFlow.sln` and `src/TaskFlow.API/`
  exist
- [ ] `dotnet build` exits 0 on the current solution
- [ ] `src/TaskFlow.API/TaskFlow.API.csproj` targets `net10.0` and has no `WeatherForecast`
  boilerplate remaining

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File | Lines | Why |
| --- | --- | --- |
| `docs/architecture/api-contract.md` | 85-102 | Exact `GET /health` request/response shape (`status`, `liveSince`, `db`) |
| `docs/architecture/clean-architecture.md` | 95-143 | Confirms `Program.cs` lives in `TaskFlow.API` and is the composition root |
| `docs/epics/EP01-engineering-addenda.md` | 141-151 | Batch Plan — confirms Batch 0 scope: appsettings wiring, fail-fast env var validation |
| `README.md` | 30-64 | Version Manifest — `Microsoft.Extensions.Diagnostics.HealthChecks` is built-in, no version to pin |
| `AGENTS.md` | 234-244 | TASKFLOW-BUILD-PIPELINE compact rule — env var fail-fast rule |

## 5. Deliverables

### Files to Create

| File Path | Contents |
| --- | --- |
| `src/TaskFlow.API/appsettings.json` | Base config (non-secret defaults, empty connection string placeholder) |
| `src/TaskFlow.API/Configuration/EnvVarValidator.cs` | Static helper that reads required env vars and throws `InvalidOperationException` naming the missing var if absent |

### Files to Modify

| File Path | Change |
| --- | --- |
| `src/TaskFlow.API/Program.cs` | Composition root: call env var validation first, build connection string from `DB_HOST`/`DB_PORT`/`DB_USER`/`DB_PASSWORD`/`DB_NAME`, register health checks (including a PostgreSQL check), map `GET /health`, configure Kestrel to listen on `API_PORT` |

### Expected Signatures (when precision matters)

`GET /health` response shape (from
[API Contract](../../architecture/api-contract.md#health-endpoint-public)):

```jsonc
{
  "status": "ok",
  "liveSince": "2026-07-10T12:00:00Z",
  "db": "ok"
}
```

- Route is `/health`, outside the `/api` prefix, no authentication.
- `status` is always `"ok"` if the process is running and able to respond.
- `db` is `"ok"` if PostgreSQL responds, `"down"` if unreachable — the endpoint itself must
  still return `200 OK` in the `"down"` case, it never crashes or 5xxs because the DB is
  unavailable.
- `liveSince` is the ISO 8601 timestamp of process start (capture `DateTimeOffset.UtcNow` once
  at startup and reuse it).

`EnvVarValidator` — required variables to validate at startup: `DB_HOST`, `DB_PORT`, `DB_USER`,
`DB_PASSWORD`, `DB_NAME`, `API_PORT`, `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`. On missing
var, throw with a message naming the exact variable, e.g.
`"Missing required environment variable: JWT_SECRET"`.

`appsettings.json` reads connection details and JWT settings from configuration sections that
`Program.cs` populates from env vars (options pattern) — do not read `IConfiguration` directly
inside endpoint handlers.

`Program.cs` — Kestrel must bind to the port from `API_PORT` (e.g.
`builder.WebHost.UseUrls($"http://0.0.0.0:{apiPort}")` or equivalent configuration-driven
binding), not a hardcoded port.

Leave clearly marked placeholder comments in `Program.cs` for future DI registration (auth
middleware, EF Core `DbContext`) — do not implement them now.

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
| --- | --- | --- | --- |
| G1 | Compilation | `dotnet build` | exit 0 |
| G2 | App starts without DB | `dotnet run --project src/TaskFlow.API` (with `db` container stopped) | process starts, no unhandled exception, stays up |
| G3 | Health endpoint — DB down | `curl -s http://localhost:5000/health` (DB stopped) | `200 OK`, JSON with `"status":"ok"`, `"db":"down"` |
| G4 | Health endpoint — DB up | `docker compose up db -d && curl -s http://localhost:5000/health` | `200 OK`, JSON with `"status":"ok"`, `"db":"ok"` |
| G5 | Fail-fast on missing env var | unset `JWT_SECRET` and run `dotnet run --project src/TaskFlow.API` | process exits non-zero with error message naming `JWT_SECRET` |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Add authentication/authorization middleware — no `[Authorize]`, no JWT validation logic yet
- Create an EF Core `DbContext` or register `AddDbContext` — the health check uses
  `AspNetCore.HealthChecks.NpgSql` (not raw `NpgsqlConnection`), not the full ORM
- Add any controller beyond the health endpoint
- Register any entity or repository
- Read `IConfiguration` directly inside a controller or minimal-API handler — always go through
  the options pattern

### SCOPE BOUNDARY — Stop when

- `GET /health` returns the exact contract shape in both DB-up and DB-down states
- Fail-fast validation covers all 9 required env vars listed in Section 5
- All quality gates in Section 6 pass
- Do NOT proceed to auth, EF Core, or task/user endpoints — those are later batches

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
| --- | --- | --- |
| Hardcoding the connection string in `appsettings.json` | Violates env-var-driven config and leaks dev credentials | Build the connection string in `Program.cs` from `DB_HOST`/`DB_PORT`/`DB_USER`/`DB_PASSWORD`/`DB_NAME` |
| Letting the health endpoint throw/500 when DB is unreachable | Contradicts the contract — `db: "down"` must still be a `200 OK` response | Wrap the DB probe in a try/catch (or use the health check's built-in degraded/unhealthy handling) and map it to `"down"` without propagating the exception |
| Adding `[Authorize]` to `/health` "for consistency" | Contradicts the API Contract — `/health` is explicitly public, used by Docker `HEALTHCHECK` before auth is even relevant | Leave the endpoint fully anonymous |
| Reading env vars ad hoc inside the health check handler | Scatters config logic, bypasses fail-fast validation at startup | Validate and bind all env vars once in `Program.cs` via `EnvVarValidator` and the options pattern |
| Registering an EF Core `DbContext` for the health check | Out of scope — pulls in ORM machinery a batch early | Use `AspNetCore.HealthChecks.NpgSql` package — no raw `NpgsqlConnection` |

## 9. Rollback Guidance

If quality gates fail after implementation:

1. Read the error output — identify which gate failed
2. If G1 (compilation) fails: fix the syntax/type error
3. If G2 (app won't start without DB) fails: the health check or startup code is likely
   throwing synchronously on a failed DB probe instead of catching it — isolate the DB check
   inside its own try/catch
4. If G3/G4 (health endpoint content) fail: compare the actual JSON against the contract in
   [API Contract — Health Endpoint](../../architecture/api-contract.md#health-endpoint-public)
   field-by-field
5. If G5 (fail-fast) fails: confirm `EnvVarValidator` runs before `builder.Build()` /
   `app.Run()`, not after
6. If the same gate fails 3 times: STOP. Report FAILED with gate name and error.

## 10. Compact Rules

Project standards injected by the orchestrator. Follow these exactly. These override any
default behavior or training-data conventions.

### TASKFLOW-BUILD-PIPELINE

- Env vars come from `.env` file, validated at startup — fail-fast with named error on missing
  vars
- PostgreSQL is the ONLY database engine — no EF Core InMemory, no SQLite, no in-memory
  substitutes
- Same pipeline in every environment: local, CI, Docker — no environment-specific shortcuts

### TASKFLOW-ANTI-DRIFT

- Respect the current phase — do not jump ahead (no auth, no EF Core yet)
- Every decision must trace back to a requirement or acceptance criterion
- Version pinning: ALL dependencies use exact versions, never floating (no `^`, no `~`, no
  `latest`)

### TASKFLOW-TEST-HARNESS

- ALL tests must pass before any commit
- Backend: integration tests at API level (AAA pattern: Arrange/Act/Assert) — a future batch
  adds the integration test asserting `/health` shape; this task only needs the endpoint to
  behave correctly under manual `curl` verification

## 11. Status Protocol

Include this block EXACTLY in your final response. No variations.

```text
Status: [DONE | FAILED | BLOCKED]
Progress: X/Y items (items = deliverables from Section 5)
Quality Gates: G1:PASS G2:PASS G3:PASS G4:PASS G5:PASS (or FAIL with error)
Blocker: (if BLOCKED — describe exactly what prevents progress)
Files Created: (list of new files)
Files Modified: (list of changed files)
```
