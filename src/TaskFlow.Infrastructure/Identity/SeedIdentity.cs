namespace TaskFlow.Infrastructure.Identity;

public static class SeedIdentity
{
    /// <summary>Single source of truth for the Delivery-1 seed owner.
    /// Referenced by DI registration, SeedCurrentUserContext, and all test fixtures.
    /// </summary>
    public static readonly Guid SeedOwnerId = Guid.Parse("01961234-5678-7abc-def0-123456789abc");

    /// <summary>Second seed owner, used exclusively by integration tests to
    /// prove ownership isolation (AC-007.5): a task owned by this id must be
    /// invisible/unmodifiable through the HTTP API, whose active principal is
    /// always <see cref="SeedOwnerId"/> (see <see cref="SeedCurrentUserContext"/>).
    /// </summary>
    public static readonly Guid SeedOwnerId2 = Guid.Parse("02961234-5678-7abc-def0-123456789abc");

    /// <summary>Email claim/DB row value paired with <see cref="SeedOwnerId"/>.
    /// Used by integration tests to generate JWTs and to seed the matching
    /// "users" row so GET /api/auth/me resolves to a real row.</summary>
    public const string SeedOwnerEmail = "seed-owner@test.taskflow.local";

    /// <summary>Name claim/DB row value paired with <see cref="SeedOwnerId"/>.</summary>
    public const string SeedOwnerName = "Seed Owner";

    /// <summary>Email claim/DB row value paired with <see cref="SeedOwnerId2"/>.</summary>
    public const string SeedOwnerId2Email = "seed-owner-2@test.taskflow.local";

    /// <summary>Name claim/DB row value paired with <see cref="SeedOwnerId2"/>.</summary>
    public const string SeedOwnerId2Name = "Seed Owner 2";
}
