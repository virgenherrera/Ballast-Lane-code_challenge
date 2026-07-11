> [📚 INDEX](../INDEX.md) / [Process](../process.md) / Handoff Template

# Task Handoff Template

Structured delegation format for autonomous sub-agent execution. Each handoff file is a
self-contained contract: the sub-agent reads it, executes it, and reports back through the
status protocol. No improvisation, no ad-hoc prompts, no ambiguity.

## Design Principles

| Principle | Failure Mode Prevented | How |
| --------- | ---------------------- | --- |
| Self-containment | Context overflow / hallucination | Sub-agent reads ONLY the handoff + referenced files. No searching the codebase, no guessing conventions |
| Deterministic verification | Subjective self-assessment | Every quality gate is a shell command returning exit 0 (pass) or non-zero (fail). No "code should be clean" |
| Explicit boundaries | Scope creep | OUT OF SCOPE list prevents agents from "helpfully" refactoring, adding features, or reorganizing files |
| Context budget | Context window exhaustion | Context bundle lists exact files with line ranges. Every reference justifies its inclusion |
| Rollback safety | Compounding errors | 3-failure circuit breaker prevents infinite fix loops. Revert to known-good state instead |
| Structured status | Unparseable responses | Machine-readable status block forces binary DONE/FAILED/BLOCKED. No "I think it mostly works" |

## Template

The orchestrator fills every `{placeholder}` before delegation. Sections marked **(required)**
must always be present; sections marked **(optional)** may be omitted when genuinely not
applicable.

---

```markdown
# Handoff: {TASK-ID} — {Task Name}

## 1. Metadata                                              (required)

| Field         | Value                                     |
| ------------- | ----------------------------------------- |
| Task ID       | {TASK-ID} (e.g., EP01-B3-01)              |
| Task Name     | {short descriptive name}                  |
| Batch         | {batch number} of {total batches}         |
| Epic          | {epic ID and name}                        |
| User Stories  | {US-XXX, US-YYY} (only the ones covered)  |
| Persona       | {name — expertise}                        |
| Model Tier    | {sonnet | haiku}                          |

## 2. Objective                                             (required)

{1-3 sentences: what this task produces and why it matters.
 State the deliverable, not the process. The sub-agent should
 be able to read this and know exactly what "done" looks like.}

## 3. Pre-Conditions                                        (required)

These MUST be true before the sub-agent starts work.
If any pre-condition fails, report BLOCKED immediately.

- [ ] {Pre-condition 1: e.g., "dotnet build exits 0"}
- [ ] {Pre-condition 2: e.g., "file src/Domain/Entities/User.cs exists"}
- [ ] {Pre-condition 3: e.g., "dotnet test --filter Category=Unit exits 0"}

## 4. Context Bundle                                        (required)

Read ONLY these files. Do not explore beyond this list.
Line ranges narrow the read window when the full file is not needed.

| File                          | Lines   | Why                            |
| ----------------------------- | ------- | ------------------------------ |
| {path/to/file1.cs}            | {all}   | {reason for reading}           |
| {path/to/file2.md}            | {42-87} | {reason, e.g., "AC defs"}     |
| {path/to/file3.cs}            | {1-30}  | {reason, e.g., "interface"}   |

## 5. Deliverables                                          (required)

Exact files to create or modify. Include expected structure
where it reduces ambiguity (namespace, class name, method signatures).

### Files to Create

| File Path                     | Contents                               |
| ----------------------------- | -------------------------------------- |
| {src/Domain/Entities/Task.cs} | {Entity with properties: Id, Title...} |
| {tests/Domain.Tests/Tests.cs} | {Unit tests for entity invariants}     |

### Files to Modify

| File Path                     | Change                                 |
| ----------------------------- | -------------------------------------- |
| {src/Infra/Persistence/Db.cs} | {Add DbSet<Task> property}             |

### Expected Signatures (when precision matters)

```csharp
// Include method signatures, constructor shapes, or interface
// implementations when the sub-agent must match a specific contract.
```

## 6. Quality Gates                                         (required)

Run these commands IN ORDER after implementation.
ALL must exit 0. If any fails, apply rollback guidance (Section 9).

| #  | Gate                | Command                                | Pass Criteria        |
| -- | ------------------- | -------------------------------------- | -------------------- |
| G1 | {Compilation}       | {`dotnet build`}                       | {exit 0}             |
| G2 | {Unit tests}        | {`dotnet test --filter Category=Unit`} | {exit 0, 0 failures} |
| G3 | {Integration tests} | {`dotnet test --filter Category=Int`}  | {exit 0, 0 failures} |
| G4 | {Format check}      | {`dotnet format --verify-no-changes`}  | {exit 0}             |

## 7. Boundaries                                            (required)

### OUT OF SCOPE — Do NOT:

- {Modify any file not listed in Section 5}
- {Add NuGet packages not already in the solution}
- {Implement features from other user stories}
- {Refactor existing code unless it blocks a quality gate}
- {Change project references or solution structure}

### SCOPE BOUNDARY — Stop when:

- {All deliverables in Section 5 are created/modified}
- {All quality gates in Section 6 pass}
- {Do NOT proceed to the next batch's work}

## 8. Anti-Patterns                                         (optional)

Common mistakes a sub-agent might make on this type of task.

| Anti-Pattern                  | Why It Fails                   | Do Instead                     |
| ----------------------------- | ------------------------------ | ------------------------------ |
| {e.g., Using EF InMemory}     | {Skips real SQL constraints}   | {Use real PostgreSQL}          |
| {e.g., Catching all exns}     | {Swallows domain errors}       | {Let domain exns propagate}    |
| {e.g., Adding unused deps}    | {Violates version-pinning}     | {Only add what is consumed}    |

## 9. Rollback Guidance                                     (required)

If quality gates fail after implementation:

1. {Read the error output — identify which gate failed}
2. {If G1 (compilation) fails: fix the syntax/type error}
3. {If G2/G3 (tests) fail: check if your code broke an existing test vs. new test}
   - Existing test broke → {revert your change, find alternative approach}
   - New test fails → {fix the implementation, not the test}
4. {If the same gate fails 3 times: STOP. Report FAILED with gate name and error.}

## 10. Compact Rules                                        (required)

Project standards injected by the orchestrator. Follow these exactly.
These override any default behavior or training-data conventions.

{Paste the relevant TASKFLOW-* compact rule blocks here.
 Only include rules relevant to this task type.}

## 11. Status Protocol                                      (required)

Include this block EXACTLY in your final response. No variations.

```text
Status: [DONE | FAILED | BLOCKED]
Progress: X/Y items (items = deliverables from Section 5)
Quality Gates: G1:PASS G2:PASS G3:PASS G4:PASS (or FAIL with error)
Blocker: (if BLOCKED — describe exactly what prevents progress)
Files Created: (list of new files)
Files Modified: (list of changed files)
```
```

---

## Task Type Adaptations

The template is the same for all task types, but sections flex in emphasis:

| Task Type | Heavy Sections | Light Sections | Key Risk |
| --------- | -------------- | -------------- | -------- |
| Scaffolding / Infra | Deliverables, Boundaries | Anti-Patterns | Agent adds unnecessary packages |
| Domain Modeling | Expected Signatures, Anti-Patterns | Context Bundle | Agent leaks infrastructure into domain |
| API Endpoint | Context Bundle, Quality Gates | Rollback | Agent diverges from API contract |
| Testing | Context Bundle, Anti-Patterns | Deliverables | Agent uses mocks where real DB is required |
| Frontend | Deliverables, Boundaries, Anti-Patterns | Pre-Conditions | Agent over-engineers component structure |

## Quality Gate Design Rules

- **Copy-pasteable**: every command must run as-is in a terminal
- **Ordered by speed**: compilation first (fastest feedback), then unit, then integration, then format
- **Idempotent**: running the same gate twice produces the same result
- **No external dependencies**: gates should not require network access beyond local Docker containers

## Orchestrator Pre-Flight Checklist

Before handing a file to a sub-agent, the orchestrator verifies:

| # | Check | If Failed |
| - | ----- | --------- |
| 1 | All `{placeholders}` are filled — no template variables remain | Sub-agent will hallucinate missing values |
| 2 | Every context bundle file exists at the specified path | Sub-agent will report BLOCKED or read wrong files |
| 3 | Every quality gate command can be run from the repo root | Gate becomes uncheckable, PDC fails |
| 4 | Pre-conditions have been independently verified | Sub-agent builds on broken foundation |
| 5 | Boundaries explicitly name at least 3 things OUT of scope | Scope creep will occur |
| 6 | Compact rules are pasted inline, not referenced by path | Sub-agent cannot read external files not in bundle |
| 7 | The handoff is under 300 lines (excluding compact rules) | Task is too large — split it |
| 8 | Deliverables list every file to create AND modify | Sub-agent omits files or creates unexpected ones |

If the handoff exceeds 300 lines, the task is too large. Split it into sub-tasks with
separate handoffs.

## Related Documents

- [Process Protocols](../process.md) — DOR, DOD, grooming, completion, handoff ceremonies
- [AGENTS.md](../../AGENTS.md) — delegation contract, PDC, status protocol, compact rules
- [Testing Strategy](../architecture/testing-strategy.md) — named test cases referenced in context bundles
- [API Contract](../architecture/api-contract.md) — endpoint specs referenced in context bundles
