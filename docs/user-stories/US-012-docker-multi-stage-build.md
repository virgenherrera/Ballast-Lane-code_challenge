> [📚 INDEX](../INDEX.md) / [EP00](../epics/EP00-project-infrastructure.md) / US-012

# US-012 — Docker Multi-Stage Build

> **Pinned versions**: [README — Version Manifest](../../README.md#version-manifest)

**Epic**: EP00 - Project Infrastructure
**Dependencies**: None (first story in EP00)
**Priority**: Must Have
**Status**: [x] Done (documentation/planning)

## Story

As a **developer/evaluator**, I want **the project to build in a multi-stage Docker pipeline** so
that **tests run automatically and the final image is minimal**.

## Acceptance Criteria

- [ ] **AC-012.1: Build stage compiles with the full SDK**
  - **Given** the backend Dockerfile's build stage
  - **When** the image is built
  - **Then** the .NET solution is restored and compiled using the full .NET SDK base image

- [ ] **AC-012.2: Test stage gates the build**
  - **Given** the backend Dockerfile's test stage, chained after the build stage
  - **When** the image is built
  - **Then** all integration tests run inside the container, and the build fails immediately if
    any test fails

- [ ] **AC-012.3: Runtime stage uses a minimal image**
  - **Given** the backend Dockerfile's final runtime stage
  - **When** the image is built after the test stage passes
  - **Then** the runtime stage is based on the minimal ASP.NET runtime image, with no SDK included

- [ ] **AC-012.4: Final image excludes source code**
  - **Given** the completed runtime image
  - **When** its filesystem is inspected
  - **Then** it contains only the published binary and its runtime dependencies, not the `.cs`
    source files, test projects, or the SDK

- [ ] **AC-012.5: Frontend builds in its own multi-stage pipeline**
  - **Given** the frontend Dockerfile
  - **When** the image is built
  - **Then** it uses a Node stage to install dependencies and build the Angular app, followed by
    an Nginx stage that serves only the compiled static assets

- [ ] **AC-012.6: Runtime image exposes a health endpoint**
  - **Given** the backend runtime image, configured with `Microsoft.Extensions.Diagnostics.HealthChecks`
  - **When** a `GET /health` request is made to the running API container
  - **Then** it returns `200` with a JSON body `{ "status": "ok", "liveSince": "<ISO 8601>", "db": "ok" }` when the application is running and PostgreSQL responds to a simple query; if the database is unreachable, `db` returns `"down"` and the Dockerfile `HEALTHCHECK` instruction uses this endpoint to determine container health

## Notes

- The test stage must run the same integration test suite defined in `testing-strategy.md`
  (Section 3), not a reduced subset.
- A failed test stage must produce a non-zero exit code so CI and local builds both stop at that
  point — no runtime image is produced from a red test run.
- Frontend and backend each get their own independent multi-stage Dockerfile; neither depends on
  build artifacts from the other during the image build itself (composition happens later, at the
  Docker Compose level).

## Related Documents

- [Build Pipeline](../architecture/build-pipeline.md) — deterministic 5-stage gated pipeline that
  produces the images this Dockerfile builds
- [Tech Stack — Decision 7: Docker Strategy](../architecture/tech-stack.md#decision-7-docker-strategy)
  — rationale for the multi-stage build approach
- [Testing Strategy](../architecture/testing-strategy.md) — integration test suite gated by the
  test stage
- [US-013 — Docker Compose Environment](US-013-docker-compose-environment.md) — orchestrates the
  images produced by this build
- [EP00 — Project Infrastructure](../epics/EP00-project-infrastructure.md) — parent epic
