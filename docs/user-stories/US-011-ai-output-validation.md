> [📚 INDEX](../INDEX.md) / [EP03 — GenAI Process Documentation](../epics/EP03-genai-documentation.md) / US-011

# US-011 — AI Output Validation Report

**Epic**: [EP03 - GenAI Process Documentation](../epics/EP03-genai-documentation.md)
**Priority**: Must Have
**Status**: [x] Complete

## Story

As an **interview panel member**, I want to **see how the candidate validated and improved
AI-generated code** so that **I can evaluate their critical thinking and technical judgment**.

## Acceptance Criteria

- [x] **AC-011.1: Output shown**
  - **Given** the AI generated code output
  - **When** the panel reviews the documentation
  - **Then** the AI's output (or representative sample) is clearly shown

- [x] **AC-011.2: Validation process**
  - **Given** the AI output documentation
  - **When** the panel reviews it
  - **Then** the candidate describes how they validated the code against requirements

- [x] **AC-011.3: Corrections documented**
  - **Given** the AI output needed corrections
  - **When** the panel reviews the documentation
  - **Then** specific corrections are listed with technical reasoning for each

- [x] **AC-011.4: Edge cases and security**
  - **Given** the complete documentation
  - **When** the panel reviews it
  - **Then** the candidate explains how edge cases, authentication, and input validation were handled

- [x] **AC-011.5: Critical assessment**
  - **Given** the complete documentation
  - **When** the panel evaluates it
  - **Then** the candidate demonstrates they did not blindly accept AI output but exercised
    technical judgment

## Validation Methodology

All AI-generated code was validated through three complementary layers:

1. **Integration tests as the primary quality gate** — Testcontainers spins up a real PostgreSQL
   instance per test run, so tests exercise the actual EF Core mappings, constraints, and
   precision behavior instead of an in-memory approximation.
2. **Manual code review** focused on security, correctness, and Clean Architecture compliance
   (dependency direction, layer boundaries).
3. **Adversarial review sessions** to deliberately stress-test edge cases the AI's first pass
   tends to skip — asymmetric validation rules, silent framework defaults, and cross-layer leakage
   of sensitive values.

## Scenario 1: PasswordHash Value Object — Preventing Accidental Exposure

- **AI Output**: The first generated `PasswordHash` value object followed the common C# pattern of
  overriding `ToString()` to return the wrapped value — convenient for debugging, dangerous for a
  hash.
- **Issue Found**: A `ToString()` override on `PasswordHash` means the BCrypt hash could leak
  through string interpolation, logging frameworks, exception messages, or debugger watch windows
  — any of which routinely call `ToString()` implicitly.
- **Correction**: Deliberately removed the `ToString()` override, so the type falls back to
  `Object.ToString()` (which prints the type name, not the value). The value object exposes the
  hash only through an explicit `.Value` property:

  ```csharp
  public sealed class PasswordHash
  {
      public string Value { get; }

      private PasswordHash(string value) => Value = value;

      public static PasswordHash Create(string hashedValue)
      {
          if (string.IsNullOrWhiteSpace(hashedValue))
              throw new InvalidPasswordHashException("Password hash is required.");
          return new PasswordHash(hashedValue);
      }

      // Deliberately DOES NOT override ToString() to expose Value — inherits
      // Object.ToString() so accidental interpolation/logging never leaks the
      // hash. Do NOT add a ToString() override.
  }
  ```

- **Validation**: Confirmed no code path in the solution calls `ToString()` on a `PasswordHash`
  instance; every access to the raw hash goes through the explicit `.Value` property, which makes
  hash access a deliberate act instead of an accidental side effect of logging or interpolation.

## Scenario 2: JWT Claim Mapping — Silent ASP.NET Core Behavior

- **AI Output**: A standard JWT bearer authentication setup, with `JwtCurrentUserContext` reading
  the authenticated user's ID from the `"sub"` claim via `FindFirst("sub")`.
- **Issue Found**: ASP.NET Core's `JwtBearerOptions.MapInboundClaims` defaults to `true`, which
  silently rewrites well-known short claim names (like `"sub"`) to their long `ClaimTypes` URI
  equivalents (`ClaimTypes.NameIdentifier`) during token validation. This is undocumented in the
  obvious places and does not surface as a compile-time or even test-time failure if the test
  harness builds `ClaimsPrincipal` objects directly instead of round-tripping through the real JWT
  handler — the literal `"sub"` lookup in `JwtCurrentUserContext` would return `null` for every
  real, validly-signed token in production.
- **Correction**: Added `options.MapInboundClaims = false;` in the `AddJwtBearer` configuration
  block in `Program.cs`, preserving the original claim names exactly as issued by
  `JwtTokenService`:

  ```csharp
  .AddJwtBearer(options =>
  {
      var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
      options.MapInboundClaims = false;
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwtOptions.Issuer,
          ValidAudience = jwtOptions.Audience,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
          ClockSkew = TimeSpan.Zero,
      };
  });
  ```

- **Validation**: The integration test suite exercises the full chain — real token generation via
  `JwtTokenService`, real JWT bearer validation middleware, and `JwtCurrentUserContext` resolution
  — against a live `WebApplicationFactory` pipeline, so this class of "claim name silently changed
  under me" bug cannot pass undetected the way it would with a hand-built `ClaimsPrincipal` in a
  unit test.

## Scenario 3: Due Date Validation Asymmetry (Create vs Update)

- **AI Output**: The first `Update`/`Reschedule` implementation reused the same future-date
  validation rule the AI had just written for `Create` — a natural instinct toward consistency, but
  wrong here.
- **Issue Found**: If a task is created with a due date 3 days out, and the same "due date must be
  in the future" rule applies on every update, the task becomes permanently un-editable after
  4 days — its own due date is now in the past, and the validator rejects the update even when the
  user isn't touching the due date field at all. Symmetric validation created an unintended
  dead-end state.
- **Correction**: `TaskItem.Create()` enforces `dueDate > DateTime.UtcNow` (prevents a task from
  ever starting life already overdue). `TaskItem.Reschedule()` intentionally omits that check —
  past due dates are allowed on update, because editing a task that has drifted into the past must
  remain possible:

  ```csharp
  public void Reschedule(DateTime? dueDate)
  {
      // CRITICAL ASYMMETRY: past dates EXPLICITLY ALLOWED here (unlike Create).
      // See TaskItemTests.Task_CreateWithPastDueDate_ThrowsDomainException for the
      // Create-side rejection this method intentionally does NOT replicate (US-007).
      DueDate = dueDate;
      UpdatedAt = UtcNowTruncated();
  }
  ```

- **Validation**: Acceptance criterion AC-007.11 explicitly tests that editing a task whose due
  date has already elapsed succeeds rather than throwing, closing off the dead-end state.

## Scenario 4: PostgreSQL Timestamp Precision Mismatch

- **AI Output**: Domain methods stamped `CreatedAt`/`UpdatedAt` directly from `DateTime.UtcNow`.
- **Issue Found**: .NET's `DateTime.UtcNow` carries 100-nanosecond tick precision (7 fractional
  digits), while PostgreSQL's `timestamp with time zone` column stores microsecond precision
  (6 fractional digits). An entity returned immediately after `Create`/`Update` — before any
  round trip through the database — would serialize with 7-digit precision, while a subsequent
  `GET` of the same row reads back the DB-truncated 6-digit value. Two JSON representations of what
  should be the exact same instant, differing only in whether the object had been persisted and
  reloaded yet — a nondeterministic-looking failure in integration tests that compare an
  in-memory response against a freshly-fetched one.
- **Correction**: Added a `UtcNowTruncated()` helper in `TaskItem.cs` that truncates to microsecond
  precision at the source, before the value is ever assigned:

  ```csharp
  private const long TicksPerMicrosecond = 10;

  private static DateTime UtcNowTruncated()
  {
      var now = DateTime.UtcNow;
      return now.AddTicks(-(now.Ticks % TicksPerMicrosecond));
  }
  ```

- **Validation**: All timestamp-sensitive integration tests (comparing `CreatedAt`/`UpdatedAt`
  across the create-response vs. get-response boundary) pass consistently — the truncation removes
  the precision mismatch at the domain layer instead of papering over it with test-side tolerance
  windows.

## Critical Thinking Summary

AI-generated code in this project was validated at three levels:

1. **Functional correctness** — Does it work? (Integration tests against real PostgreSQL via
   Testcontainers.)
2. **Security review** — Does it leak information? Does it resist known attack patterns? (Manual
   review — password hash exposure, timing attacks, user enumeration via error messages.)
3. **Platform awareness** — Does it account for framework-specific behaviors the AI's training
   distribution treats as "standard" but which are actually configurable defaults with sharp
   edges? (ASP.NET Core's silent claim remapping, PostgreSQL's timestamp precision ceiling.)

The most dangerous class of AI-generated bug is the one that passes every test yet fails silently
in production. `MapInboundClaims` is the clearest example in this codebase: the code compiles
cleanly, and any test that constructs a `ClaimsPrincipal` by hand (bypassing the real JWT
validation pipeline) would pass without ever exercising the remapping behavior — while a real
client presenting a real, validly-signed token would get a `401` on every protected endpoint. This
is precisely the class of gap that unblinking acceptance of AI output does not catch, and why every
scenario above was closed with an integration test that exercises the real pipeline end-to-end, not
just the unit under review.

## Related Documents

- [Testing Strategy — Harness Rules](../architecture/testing-strategy.md#8-harness-rules) — validation standard AI output must meet
- [US-010 — Prompt Documentation](US-010-prompt-documentation.md) — companion deliverable for this epic
