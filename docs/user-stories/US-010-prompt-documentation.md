> [📚 INDEX](../INDEX.md) / [EP03 — GenAI Process Documentation](../epics/EP03-genai-documentation.md) / US-010

# US-010 — Prompt Documentation

**Epic**: [EP03 - GenAI Process Documentation](../epics/EP03-genai-documentation.md)
**Priority**: Must Have
**Status**: [x] Complete

## Story

As an **interview panel member**, I want to **see the exact prompts used with AI tools** so that **I can evaluate the candidate's prompt engineering skills**.

## Acceptance Criteria

- [x] **AC-010.1: Prompt visibility**
  - **Given** the candidate used a GenAI tool to generate API code
  - **When** the panel reviews the documentation
  - **Then** the exact prompt(s) used are clearly documented

- [x] **AC-010.2: Tool identification**
  - **Given** the documentation includes prompts
  - **When** the panel reviews them
  - **Then** the specific AI tool used is identified (e.g., Claude Code, Cursor, Copilot)

- [x] **AC-010.3: Context and intent**
  - **Given** the prompt documentation
  - **When** the panel reviews it
  - **Then** the candidate's intent behind each prompt is explained

## Tool Used

**Claude Code CLI** — Anthropic's official command-line AI coding assistant, running the
**Claude Opus** model. Used across the full lifecycle of this project: discovery, epic/user-story
grooming, architecture documentation, TDD implementation, and this GenAI documentation itself.

The project was built as a sequence of **vertical slices** (see `git log --oneline`), each pairing
a planning commit (`docs(...)`) with an implementation commit (`feat(...)`), so every prompt below
maps to a real, inspectable commit rather than a hypothetical example.

## Prompt Categories and Examples

### Category 1: Architecture & Project Scaffolding

- **Intent**: Establish a Clean Architecture project structure before any feature code, so every
  subsequent slice has a stable dependency graph to slot into.
- **Example prompt**:

  > "Create a .NET 10 solution following Clean Architecture with four layers: Domain (entities,
  > interfaces), Application (use cases, DTOs, validation), Infrastructure (EF Core, JWT,
  > repositories), and API (controllers, middleware). Use PostgreSQL with Testcontainers for
  > integration tests."

- **Refinement**: The first scaffold leaked EF Core types into `Domain` through a convenience
  base class. Follow-up prompt: "Domain must not reference Infrastructure or any EF Core package —
  verify with a `.csproj` reference check." Required two more iterations to get the dependency
  graph pointing strictly inward (Domain at the center, Infrastructure at the edges, no outward
  references from Domain or Application).

### Category 2: Feature Implementation (Vertical Slices)

- **Intent**: Implement CRUD operations end-to-end using TDD, one user story at a time
  (US-004 Create, US-007 Update, US-008 Delete, US-005/006/009 Read).
- **Example prompt**:

  > "Implement US-004 Create Task as a full-stack vertical slice: domain entity, application
  > handler with FluentValidation, repository implementation, API controller endpoint, and
  > integration tests. Follow TDD — write the integration test first."

- **Refinement**: The initial `CreateTaskCommandValidator` used FluentValidation's default
  `CascadeMode.Stop`, so only the first validation failure was ever returned — a request with both
  a missing title and an oversized description silently reported only one error. Corrected by
  setting `ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Continue` globally in
  `Program.cs`, so every rule runs and all failures are collected into a single response.

### Category 3: Authentication & Security

- **Intent**: JWT-based authentication (EP02) built to resist the standard attack surface for a
  login endpoint: user enumeration, timing attacks, brute force, and hash leakage.
- **Example prompt**:

  > "Implement user registration and login with JWT. Requirements: BCrypt password hashing (work
  > factor 12), timing-attack resistant login (always run Verify even on user-not-found), rate
  > limiting on login (5/min/IP), and generic error messages (no field hints on auth failures)."

- **Refinement**: The AI's first JWT setup relied on ASP.NET Core's default claim-mapping
  behavior, which silently rewrites the "sub" claim to the long `ClaimTypes.NameIdentifier` URI on
  inbound tokens. `JwtCurrentUserContext`'s literal `FindFirst("sub")` lookup returned `null` for
  every authenticated request, even with a structurally valid token. Fixed by explicitly setting
  `options.MapInboundClaims = false` in `Program.cs`'s `AddJwtBearer` configuration — see
  [US-011, Scenario 2](US-011-ai-output-validation.md#scenario-2-jwt-claim-mapping--silent-aspnet-core-behavior)
  for the full validation trail.

### Category 4: UI Redesign

- **Intent**: Replace the functional-but-plain initial UI with a modern, responsive task
  management interface (EP03 UI redesign).
- **Example prompt**:

  > "Redesign the task list component using a TailAdmin-inspired layout with status tabs,
  > card-based task items, inline status cycling, and server-side pagination. Use Tailwind CSS v4
  > utility classes."

- **Refinement**: The initial Tailwind integration used `@import` inside a `.scss` file, which
  triggered a Dart Sass deprecation warning (Sass's own `@import` is being phased out, and mixing
  it with Tailwind's CSS-native `@import "tailwindcss"` compounded the noise). Resolved by moving
  the Tailwind import to a CSS-native entry point instead of routing it through Sass.

## Related Documents

- [Project Brief — Deliverables](../project-brief.md#deliverables) — D3: GenAI Documentation deliverable
- [US-011 — AI Output Validation Report](US-011-ai-output-validation.md) — companion deliverable for this epic
