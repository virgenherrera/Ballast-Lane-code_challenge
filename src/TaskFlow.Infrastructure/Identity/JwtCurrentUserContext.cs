using Microsoft.AspNetCore.Http;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Identity;

/// <summary>
/// <see cref="ICurrentUserContext"/> adapter backed by the authenticated
/// request's JWT claims. Extracts the owner id from the "sub" claim — this
/// relies on <c>JwtBearerOptions.MapInboundClaims = false</c> being set in
/// Program.cs, otherwise ASP.NET Core remaps "sub" to the long
/// <see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/> URI and the
/// literal "sub" lookup below returns null.
/// Registered as scoped (see Program.cs) because <see cref="IHttpContextAccessor"/>
/// is only valid for the lifetime of a single request.
/// </summary>
public sealed class JwtCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid OwnerId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
                ?? throw new InvalidOperationException("Missing 'sub' claim. Ensure [Authorize] is applied.");
            return Guid.Parse(sub);
        }
    }
}
