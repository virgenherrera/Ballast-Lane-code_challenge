# Handoff: EP02-B0-01 — Solution Scaffold

## 1. Metadata

| Field | Value |
| --- | --- |
| Task ID | EP02-B0-01 |
| Task Name | Solution Scaffold |
| Batch | 0 of 7 |
| Epic | EP02 — User Management |
| User Stories | none (infrastructure only) |
| Persona | Uncle Bob — Clean Architecture Author |
| Model Tier | sonnet |

## 2. Objective

Create the `TaskFlow.sln` solution with 8 projects (4 source, 4 test) wired with the correct
project references per the Clean Architecture dependency rule. This is the empty skeleton every
later batch fills in — no business logic, no packages beyond template defaults.

## 3. Pre-Conditions

- [ ] `.NET 10.0 SDK` is installed and `dotnet --version` reports a 10.x version
- [ ] The repository root contains no existing `TaskFlow.sln` (fresh scaffold)
- [ ] `.gitignore` exists at repo root (may be empty or minimal)

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File | Lines | Why |
| --- | --- | --- |
| `docs/architecture/clean-architecture.md` | 95-159 | Exact project structure and reference graph to reproduce |
| `README.md` | 30-46 | Version Manifest — .NET 10.0 / ASP.NET Core 10.0 pinned versions |
| `AGENTS.md` | 228-244 | TASKFLOW-ANTI-DRIFT and TASKFLOW-BUILD-PIPELINE compact rules |

## 5. Deliverables

### Files to Create

| File Path | Contents |
| --- | --- |
| `TaskFlow.sln` | Solution file referencing all 8 projects below |
| `src/TaskFlow.Domain/TaskFlow.Domain.csproj` | classlib, `net10.0`, no references |
| `src/TaskFlow.Application/TaskFlow.Application.csproj` | classlib, `net10.0`, references Domain |
| `src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj` | classlib, `net10.0`, references Application |
| `src/TaskFlow.API/TaskFlow.API.csproj` | webapi, `net10.0`, references Application + Infrastructure |
| `tests/TaskFlow.Domain.Tests/TaskFlow.Domain.Tests.csproj` | xunit, `net10.0`, references Domain |
| `tests/TaskFlow.Application.Tests/TaskFlow.Application.Tests.csproj` | xunit, `net10.0`, references Application + Domain |
| `tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj` | xunit, `net10.0`, references API |
| `tests/TaskFlow.E2E/TaskFlow.E2E.csproj` | classlib placeholder, `net10.0`, no references (Playwright wiring is a later batch) |

### Files to Modify

| File Path | Change |
| --- | --- |
| `.gitignore` | Append .NET ignores (`bin/`, `obj/`, `.vs/`, `*.user`, `*.suo`) and project-specific ignores (`artifacts/`, `.env`, `node_modules/`, `dist/`) if not already present |

### Expected Signatures (when precision matters)

Folder layout inside each source project must match
[Clean Architecture — Section 3](../../architecture/clean-architecture.md#3-project-structure)
exactly, e.g. `src/TaskFlow.Domain/Entities/`, `src/TaskFlow.Domain/ValueObjects/`,
`src/TaskFlow.Domain/Interfaces/`, `src/TaskFlow.Domain/Exceptions/` — these subfolders may be
created empty (or omitted until Batch 1 needs them); do not populate them with placeholder
classes.

`TaskFlow.API` is generated from the `dotnet new webapi` template. After generation, delete the
default `WeatherForecast.cs` file and the `WeatherForecastController` (or minimal-API equivalent)
that ships with the template. Leave `Program.cs` otherwise close to template defaults — Batch 0
Task 3 (EP02-B0-03) rewrites it as the composition root.

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
| --- | --- | --- | --- |
| G1 | Compilation | `dotnet build` | exit 0 |
| G2 | Empty test projects pass | `dotnet test` | exit 0, 0 failures |
| G3 | Reference graph — API | `dotnet list src/TaskFlow.API/TaskFlow.API.csproj reference` | lists Application + Infrastructure |
| G4 | Reference graph — Infrastructure | `dotnet list src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj reference` | lists Application only |
| G5 | Reference graph — Application | `dotnet list src/TaskFlow.Application/TaskFlow.Application.csproj reference` | lists Domain only |
| G6 | Reference graph — Domain | `dotnet list src/TaskFlow.Domain/TaskFlow.Domain.csproj reference` | empty (no references) |
| G7 | No boilerplate | `rg -i "weatherforecast" src/TaskFlow.API` | no matches (exit 1 from rg is expected/pass) |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Add EF Core, FluentValidation, BCrypt.Net-Next, or any NuGet package beyond what the
  `classlib`, `webapi`, and `xunit` templates generate by default
- Create any entity, value object, interface, or controller — those belong to Batch 1 onward
- Write the health endpoint or modify `Program.cs` beyond template defaults (that is
  EP02-B0-03's job)
- Touch `docker-compose.yml`, `.env`, or `.env.example` (that is EP02-B0-02's job)
- Use any target framework other than `net10.0`

### SCOPE BOUNDARY — Stop when

- All 8 projects exist, build, and are wired into `TaskFlow.sln`
- All quality gates in Section 6 pass
- Do NOT proceed to writing Program.cs, health checks, or Docker files

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
| --- | --- | --- |
| Adding EF Core or auth packages "to save a step" | Violates out-of-scope and version-pinning rules; those packages belong to later batches with pinned versions | Leave `.csproj` files at template defaults, no extra `<PackageReference>` |
| Using `net9.0` or `net8.0` target framework | Contradicts the pinned .NET 10.0 in the README Version Manifest | Set `<TargetFramework>net10.0</TargetFramework>` in every `.csproj` |
| Forgetting a project reference (e.g., API not referencing Infrastructure) | Breaks the DI wiring Batch 0 Task 3 needs in `Program.cs` | Follow the reference graph exactly as diagrammed in Clean Architecture Section 4 |
| Leaving `WeatherForecast.cs` / its controller in place | Dead template code the reviewer will flag as leftover boilerplate | Delete both the model and controller/minimal-API endpoint after scaffolding |
| Populating Domain/Application folders with placeholder classes | Scope creep — Batch 1 owns entity/interface creation | Create folders empty or omit until needed |

## 9. Rollback Guidance

If quality gates fail after implementation:

1. Read the error output — identify which gate failed
2. If G1 (compilation) fails: fix the syntax/type error, most likely a missing `using` or a
   malformed `.csproj` reference path
3. If G3-G6 (reference graph) fail: run `dotnet add <project> reference <target>` for the
   missing link, or `dotnet remove <project> reference <target>` for an extraneous one
4. If G7 (boilerplate) fails: delete the flagged file(s) and any controller registration
   referencing them
5. If the same gate fails 3 times: STOP. Report FAILED with gate name and error.

## 10. Compact Rules

Project standards injected by the orchestrator. Follow these exactly. These override any
default behavior or training-data conventions.

### TASKFLOW-ANTI-DRIFT

- Respect the current phase — do not jump ahead
- Discovery phase: NO code, NO technology choices, NO architecture diagrams
- Every decision must trace back to a requirement or acceptance criterion
- Version pinning: ALL dependencies use exact versions, never floating (no `^`, no `~`, no
  `latest`)

### TASKFLOW-BUILD-PIPELINE

- Build pipeline has 5 sequential gated stages: setUp → build → test:static → test:dynamic →
  test:e2e
- Each stage gates the next — a failing stage STOPS the pipeline, no skipping allowed
- All build outputs go to `./artifacts/` (gitignored): `dist/api`, `dist/web`,
  `testReports/api|e2e`, `openApi/`
- PostgreSQL is the ONLY database engine — no EF Core InMemory, no SQLite, no in-memory
  substitutes
- All dependency versions pinned in
  [README — Version Manifest](../../../README.md#version-manifest) — single source of truth

## 11. Status Protocol

Include this block EXACTLY in your final response. No variations.

```text
Status: [DONE | FAILED | BLOCKED]
Progress: X/Y items (items = deliverables from Section 5)
Quality Gates: G1:PASS G2:PASS G3:PASS G4:PASS G5:PASS G6:PASS G7:PASS (or FAIL with error)
Blocker: (if BLOCKED — describe exactly what prevents progress)
Files Created: (list of new files)
Files Modified: (list of changed files)
```
