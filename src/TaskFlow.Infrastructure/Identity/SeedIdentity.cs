namespace TaskFlow.Infrastructure.Identity;

public static class SeedIdentity
{
    /// <summary>Single source of truth for the Delivery-1 seed owner.
    /// Referenced by DI registration, SeedCurrentUserContext, and all test fixtures.
    /// </summary>
    public static readonly Guid SeedOwnerId = Guid.Parse("01961234-5678-7abc-def0-123456789abc");
}
