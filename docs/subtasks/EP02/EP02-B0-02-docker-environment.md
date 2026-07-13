# Handoff: EP02-B0-02 — Docker Compose + Environment

## 1. Metadata

| Field | Value |
| --- | --- |
| Task ID | EP02-B0-02 |
| Task Name | Docker Compose + Environment |
| Batch | 0 of 7 |
| Epic | EP02 — User Management |
| User Stories | none (infrastructure only) |
| Persona | Kelsey Hightower — Cloud Native Pioneer |
| Model Tier | sonnet |

## 2. Objective

Stand up a reproducible local PostgreSQL 17.5 environment via `docker-compose.yml`, with a
committed `.env.example` documenting every required variable and a gitignored `.env` holding
real dev values. This is the database dependency every later batch (migrations, repositories,
integration tests) and Batch 0 Task 3's health check rely on.

## 3. Pre-Conditions

- [ ] Docker Engine 27.x+ and Docker Compose v2 are installed (`docker compose version`
  succeeds)
- [ ] No existing `docker-compose.yml` at repo root (fresh scaffold)
- [ ] Port 5432 is free on the host, or the chosen `DB_PORT` mapping does not collide

## 4. Context Bundle

Read ONLY these files. Do not explore beyond this list.

| File | Lines | Why |
| --- | --- | --- |
| `docs/epics/EP02-engineering-addenda.md` | 1-181 (full file) | Grooming decisions — no section directly defines env vars, but confirms Batch 0 scope and no auth/API containers yet |
| `docs/architecture/build-pipeline.md` | 81-93 | Stage 0 (setUp) — env var validation and `docker compose up -d db` gate this task must satisfy |
| `README.md` | 30-46 | Version Manifest — PostgreSQL 17.5 pinned, Docker Engine/Compose versions |
| `AGENTS.md` | 234-244 | TASKFLOW-BUILD-PIPELINE compact rule — Docker Compose topology and env var rules |

## 5. Deliverables

### Files to Create

| File Path | Contents |
| --- | --- |
| `docker-compose.yml` | Single `db` service, `postgres:17-alpine`, healthcheck, named volume |
| `.env.example` | Placeholder values for every env var listed below (committed) |
| `.env` | Real dev values for local use (gitignored — do not commit) |

### Files to Modify

| File Path | Change |
| --- | --- |
| `.gitignore` | Ensure `.env` is listed (should already be covered by EP02-B0-01's `.gitignore` update if that task ran first; add it here if missing) |

### Expected Signatures (when precision matters)

`docker-compose.yml` — `db` service shape:

```yaml
services:
  db:
    image: postgres:17-alpine
    container_name: taskflow-db
    environment:
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
      POSTGRES_DB: ${DB_NAME}
    ports:
      - "${DB_PORT}:5432"
    volumes:
      - taskflow-db-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER} -d ${DB_NAME}"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  taskflow-db-data:
```

`.env.example` — required variables (placeholders only, no real secrets):

```text
DB_HOST=localhost
DB_PORT=5432
DB_USER=taskflow
DB_PASSWORD=CHANGE_ME
DB_NAME=taskflow

API_PORT=5000
JWT_SECRET=CHANGE_ME_MIN_32_CHARS
JWT_ISSUER=TaskFlow
JWT_AUDIENCE=TaskFlow

WEB_PORT=4200
```

`.env` — same keys, dev values:

```text
DB_HOST=localhost
DB_PORT=5432
DB_USER=taskflow
DB_PASSWORD=TaskFlow2026!
DB_NAME=taskflow

API_PORT=5000
JWT_SECRET=<generate a 64-char random alphanumeric string>
JWT_ISSUER=TaskFlow
JWT_AUDIENCE=TaskFlow

WEB_PORT=4200
```

Generate the `JWT_SECRET` value with a secure random generator (e.g.
`openssl rand -hex 32`), not a hand-typed placeholder — it must be at least 32 characters per
[Engineering Addenda — Section 2 (JWT Configuration)](../../epics/EP02-engineering-addenda.md#2-jwt-configuration).

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
| --- | --- | --- | --- |
| G1 | Compose file validates | `docker compose config` | exit 0, no errors |
| G2 | DB container starts | `docker compose up db -d` | exit 0, container reaches `healthy` state |
| G3 | DB is ready | `docker compose exec db pg_isready -U taskflow -d taskflow` | reports `accepting connections` |
| G4 | Env files present | `test -f .env.example && test -f .env` | exit 0 |
| G5 | `.env` is gitignored | `git check-ignore .env` | exit 0 (confirms ignored) |

## 7. Boundaries

### OUT OF SCOPE — Do NOT

- Add `taskflow-api` or `taskflow-web` services to `docker-compose.yml` — those arrive in
  later batches once the API and frontend have Dockerfiles
- Write any Dockerfile — Batch 0 only needs the PostgreSQL container, sourced directly from
  the pinned `postgres:17-alpine` image
- Expose the DB port to the host without going through the `DB_PORT` env var
- Hardcode credentials directly in `docker-compose.yml` — every secret-bearing value must come
  from `.env` via `${VAR}` interpolation
- Use `postgres:latest` or any floating tag

### SCOPE BOUNDARY — Stop when

- `docker-compose.yml`, `.env.example`, and `.env` all exist and match the shapes in Section 5
- All quality gates in Section 6 pass
- Do NOT proceed to writing the health endpoint or wiring the connection string into the API
  (that is EP02-B0-03's job)

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
| --- | --- | --- |
| `image: postgres:latest` | Breaks reproducibility — the image can change under you between runs | Pin `image: postgres:17-alpine` exactly, matching the README Version Manifest |
| `POSTGRES_PASSWORD: TaskFlow2026!` hardcoded in `docker-compose.yml` | Leaks the credential into version control and couples compose file to one environment | Use `${DB_PASSWORD}` interpolated from `.env` |
| Omitting the healthcheck | Downstream services (API, migrations) can start racing an unready DB | Add a `pg_isready`-based healthcheck with retries |
| Committing `.env` | Leaks dev secrets (even placeholder-strength ones) into git history | Only commit `.env.example`; confirm `.env` is gitignored |
| Adding `taskflow-api`/`taskflow-web` services early | Out of scope for Batch 0 — no Dockerfiles exist yet for those images | Wait for the batches that introduce those Dockerfiles |

## 9. Rollback Guidance

If quality gates fail after implementation:

1. Read the error output — identify which gate failed
2. If G1 (`docker compose config`) fails: fix YAML syntax or undefined variable
   interpolation — every `${VAR}` in `docker-compose.yml` must have a matching key in `.env`
3. If G2/G3 (container start / readiness) fail: check `docker compose logs db` for the actual
   Postgres error (often a bad `POSTGRES_PASSWORD` format or a port already in use)
4. If G5 (`.env` gitignore) fails: add `.env` to `.gitignore` and re-run
5. If the same gate fails 3 times: STOP. Report FAILED with gate name and error.

## 10. Compact Rules

Project standards injected by the orchestrator. Follow these exactly. These override any
default behavior or training-data conventions.

### TASKFLOW-BUILD-PIPELINE

- Build pipeline has 5 sequential gated stages: setUp → build → test:static → test:dynamic →
  test:e2e
- Stage 0 (setUp) requires: env vars validated, `docker compose up -d db` succeeds, DB
  connection verified with retry (3x, 2s interval)
- Docker Compose topology target (full project, not this batch): 3 containers
  (`postgres:17-alpine`, `taskflow-api`, `taskflow-web`) — all from pinned images. Batch 0 delivers
  only the `db` container
- Env vars come from `.env` file, validated at startup — fail-fast with named error on missing
  vars
- `.env.example` committed with placeholders, `.env` gitignored
- All dependency versions pinned in
  [README — Version Manifest](../../../README.md#version-manifest) — single source of truth

### TASKFLOW-ANTI-DRIFT

- Version pinning: ALL dependencies use exact versions, never floating (no `^`, no `~`, no
  `latest`)
- Every decision must trace back to a requirement or acceptance criterion

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
