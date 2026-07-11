using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Identity;

// TODO(Delivery-3): Replace with a JWT-claim-backed ICurrentUserContext implementation.
// This seed shim exists ONLY for Delivery 1 — remove/replace per Delivery-3 DOD.
public sealed class SeedCurrentUserContext : ICurrentUserContext
{
    public Guid OwnerId => SeedIdentity.SeedOwnerId;
}
