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
}
