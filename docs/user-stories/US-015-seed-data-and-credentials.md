> [📚 INDEX](../INDEX.md) / [EP00](../epics/EP00-project-infrastructure.md) / US-015

# US-015 — Seed Data and Demo Credentials

> **Pinned versions**: [README — Version Manifest](../../README.md#version-manifest)

**Epic**: EP00 - Project Infrastructure
**Dependencies**: [US-012](US-012-docker-multi-stage-build.md), [US-013](US-013-docker-compose-environment.md) (requires running system)
**Priority**: Must Have
**Status**: [x] Done (documentation/planning)

## Story

As an **evaluator**, I want **pre-loaded demo data and credentials** so that **I can immediately
explore the application without manual setup**.

## Acceptance Criteria

- [ ] **AC-015.1: Demo user with known credentials**
  - **Given** a freshly started system
  - **When** the evaluator looks up the demo credentials in the README
  - **Then** at least one demo user account exists with a documented email and password that
    successfully logs in

- [ ] **AC-015.2: Demo tasks across statuses**
  - **Given** the demo user account
  - **When** the evaluator logs in and views the task list
  - **Then** pre-existing tasks are visible spanning the Pending, In Progress, and Completed
    statuses

- [ ] **AC-015.3: Seed runs automatically on first startup**
  - **Given** the system starting up for the first time (empty database)
  - **When** EF Core migrations apply
  - **Then** the seed process runs automatically afterward, with no manual command required by the
    evaluator

- [ ] **AC-015.4: Seed is idempotent**
  - **Given** a system that has already been seeded
  - **When** the application restarts (e.g., `docker compose down` followed by `docker compose up`
    without removing volumes, or a redeployment)
  - **Then** re-running the seed process does not duplicate the demo user or demo tasks

- [ ] **AC-015.5: Credentials are demo-appropriate, not production-strength**
  - **Given** the documented demo credentials
  - **When** they are reviewed
  - **Then** they are simple and clearly intended for demo/evaluation use only, not styled or
    treated as production secrets

## Notes

- Seed logic lives alongside EF Core migrations in the infrastructure layer, consistent with the
  "migrations-as-seeding" approach recorded in `tech-stack.md` (Decision 3).
- Demo credentials must be documented in the project README so the evaluator does not need to
  inspect seed code to find them.
- Idempotency is typically achieved by checking for the demo user's existence (e.g., by email)
  before inserting, rather than relying on the database being empty.

## Related Documents

- [Build Pipeline](../architecture/build-pipeline.md) — Stage 0 runs migrations then seeders on
  every startup
- [Tech Stack — Decision 3](../architecture/tech-stack.md) — "migrations-as-seeding" approach this
  story follows
- [Testing Strategy](../architecture/testing-strategy.md) — coverage for idempotent seed behavior
- [EP00 — Project Infrastructure](../epics/EP00-project-infrastructure.md) — parent epic
