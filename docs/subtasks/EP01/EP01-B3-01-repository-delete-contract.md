# Handoff: EP01-B3-01 — Repository: ITaskRepository.DeleteAsync Contract + Implementation

## 1. Metadata

| Field        | Value                                                           |
| ------------ | --------------------------------------------------------------- |
| Task ID      | EP01-B3-01                                                      |
| Task Name    | Repository: DeleteAsync interface signature + ExecuteDeleteAsync implementation |
| Batch        | 3 of N (EP01 Chunk-D)                                           |
| Epic         | EP01 — Task Management                                          |
| User Stories | US-008 (AC-008.1, AC-008.3)                                     |
| Persona      | Uncle Bob — Infrastructure / Repository Layer                    |
| Model Tier   | sonnet                                                          |

## 2. Objective

Extend `ITaskRepository` with a `DeleteAsync(Guid id, Guid ownerId, CancellationToken ct)` method returning `Task<bool>`, and implement it in `TaskRepository` using EF Core's `ExecuteDeleteAsync` with a composite `WHERE Id = @id AND OwnerId = @ownerId` predicate. This is a single-SQL hard delete with no entity materialization, no fetch-then-remove, and no `DbUpdateConcurrencyException` risk. The method returns `true` when exactly one row is deleted, `false` when zero rows match (covering both "not found" and "not owned" cases indistinguishably, which is intentional for AC-008.3 security).

## 3. Pre-Conditions

- [ ] `dotnet build` exits 0
- [ ] `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` exists and contains `AddAsync`, `GetAllAsync`, `GetByIdAsync`, `SaveChangesAsync`
- [ ] `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` exists and implements `ITaskRepository`
- [ ] No `DeleteAsync` method already exists in `ITaskRepository` or `TaskRepository`

If any pre-condition fails, report BLOCKED.

## 4. Context Bundle

| File | Lines | Why |
|------|-------|-----|
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` | all | Current interface — add `DeleteAsync` signature here |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | all | Current implementation — add `DeleteAsync` body here |
| `src/TaskFlow.Infrastructure/Persistence/AppDbContext.cs` | 1-21 | Confirm `Tasks` DbSet exists for `ExecuteDeleteAsync` |
| `src/TaskFlow.Domain/Entities/TaskItem.cs` | 1-10 | Confirm `Id` and `OwnerId` property names |

## 5. Deliverables

### Files to Create

None.

### Files to Modify

| File Path | Change |
|-----------|--------|
| `src/TaskFlow.Application/Common/Interfaces/ITaskRepository.cs` | Add `Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct);` |
| `src/TaskFlow.Infrastructure/Persistence/Repositories/TaskRepository.cs` | Implement `DeleteAsync` using `ExecuteDeleteAsync` with composite WHERE |

### Expected Signatures

```csharp
// In ITaskRepository.cs — add after SaveChangesAsync:
Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct);
```

```csharp
// In TaskRepository.cs — new method:
public async Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct)
{
    var rowsAffected = await _dbContext.Tasks
        .Where(t => t.Id == id && t.OwnerId == ownerId)
        .ExecuteDeleteAsync(ct);

    return rowsAffected > 0;
}
```

## 6. Quality Gates

| # | Gate | Command | Pass Criteria |
|---|------|---------|---------------|
| G1 | Compilation | `dotnet build` | exit 0, zero errors |
| G2 | No global query filter on TaskItem | `grep -r "HasQueryFilter" src/TaskFlow.Infrastructure/` | Zero matches for TaskItem (confirms ExecuteDeleteAsync translates cleanly) |
| G3 | Composite predicate verified | Code review: `DeleteAsync` contains single `.Where(t => t.Id == id && t.OwnerId == ownerId)` — NOT two separate queries | verified |
| G4 | No entity materialization | Code review: `DeleteAsync` does NOT call `GetByIdAsync`, `FindAsync`, `FirstOrDefaultAsync`, or `Remove()` — only `ExecuteDeleteAsync` | verified |
| G5 | Regression — all existing tests | `dotnet test` | exit 0, all existing tests pass |

## 7. Boundaries

### OUT OF SCOPE — Do NOT:

- Add a soft-delete column (`IsDeleted`, `DeletedAt`) or global query filter
- Create a new EF Core migration (schema is unchanged — Delete adds no columns)
- Add an existence pre-check via `GetByIdAsync` before the delete (TOCTOU race)
- Modify any other method in `ITaskRepository` or `TaskRepository`
- Add any NuGet package not already in the solution
- Implement the command handler, controller action, or tests (those are later handoffs)

### SCOPE BOUNDARY — Stop when:

- `ITaskRepository.DeleteAsync` signature is added
- `TaskRepository.DeleteAsync` is implemented with `ExecuteDeleteAsync`
- All quality gates pass
- Do NOT proceed to handler or controller work

## 8. Anti-Patterns

| Anti-Pattern | Why It Fails | Do Instead |
|---|---|---|
| `var entity = await _dbContext.Tasks.FindAsync(id); _dbContext.Remove(entity); await SaveChangesAsync()` | Fetch-then-remove: TOCTOU race window, `DbUpdateConcurrencyException` on concurrent delete, requires entity tracking | Single `ExecuteDeleteAsync` with composite WHERE — no fetch, no tracking |
| Splitting into two queries (find by id, then check ownerId) | Two round-trips, TOCTOU race between them, leaks existence to non-owners | Single composite predicate `Id == id && OwnerId == ownerId` in one WHERE |
| Adding `IsDeleted` column "for safety" | Violates hard-delete product decision, adds migration, adds global query filter complexity | Hard delete only — no soft-delete artifacts anywhere |
| Throwing an exception inside the repository for zero-match case | Repository is bool-returning; exception responsibility belongs to the handler | Return `false` when `rowsAffected == 0`, let handler decide what to throw |

## 9. Rollback Guidance

1. If G1 fails: likely a method signature mismatch between interface and implementation — check parameter types match exactly (`Guid id, Guid ownerId, CancellationToken ct`)
2. If G2 finds a global query filter: do NOT proceed — report BLOCKED. `ExecuteDeleteAsync` respects global filters, which could narrow the predicate unexpectedly
3. If G5 fails: the new method should not affect existing tests since it has no callers yet — check for compilation errors from interface changes
4. **3-FAILURE CIRCUIT BREAKER**: If the same gate fails 3 times, STOP and report FAILED.

## 10. Compact Rules

### TASKFLOW-TEST-HARNESS
- ALL tests must pass before any commit
- Breaking an existing test is a blocking issue
- PostgreSQL is the ONLY database engine — no InMemory/SQLite

### TASKFLOW-ANTI-DRIFT
- Every decision must trace back to a requirement or AC
- No soft-delete artifacts — hard delete only per product decision
- Repository is bool-returning, never exception-throwing for zero-match

### TASKFLOW-BUILD-PIPELINE
- PostgreSQL is the ONLY database engine — no EF Core InMemory, no SQLite
- Docker Compose: postgres:17-alpine, taskflow-api, taskflow-web
- All dependency versions pinned

## 11. Status Protocol

```text
STATUS: DONE | BLOCKED | FAILED
TASK: EP01-B3-01
FILES_CREATED: []
FILES_MODIFIED: [list]
TESTS_PASSED: {count}
TESTS_FAILED: {count}
NOTES: {confirm G2 no global query filter; confirm G3 single composite predicate; confirm G4 no entity materialization}
```
