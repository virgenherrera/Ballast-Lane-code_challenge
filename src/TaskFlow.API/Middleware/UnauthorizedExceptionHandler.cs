using Microsoft.AspNetCore.Diagnostics;

namespace TaskFlow.API.Middleware;

/// <summary>
/// Project-wide reusable exception handler (.NET 8+ <see cref="IExceptionHandler"/>
/// pattern) that catches the fallback case where <see cref="TaskFlow.Infrastructure.Identity.JwtCurrentUserContext"/>
/// throws due to a missing "sub" claim. Under normal operation the JWT bearer
/// middleware (<c>JwtBearerEvents.OnChallenge</c> in Program.cs) rejects
/// unauthenticated/invalid-token requests before a controller action ever
/// runs, so this handler is a defensive fallback rather than the primary
/// 401 path. Maps to the standard error response shape:
/// <code>
/// {
///   "status": 401,
///   "error": "UNAUTHORIZED",
///   "message": "Missing, invalid, or expired authentication token.",
///   "details": []
/// }
/// </code>
/// Registered via <c>AddExceptionHandler&lt;UnauthorizedExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c> in Program.cs.
/// </summary>
public sealed class UnauthorizedExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        // This handler catches the case where JwtCurrentUserContext throws
        // due to missing claims (fallback; normally JWT middleware rejects first).
        if (exception is not InvalidOperationException ioe
            || !ioe.Message.Contains("sub"))
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 401,
            error = "UNAUTHORIZED",
            message = "Missing, invalid, or expired authentication token.",
            details = Array.Empty<object>(),
        }, ct);
        return true;
    }
}
