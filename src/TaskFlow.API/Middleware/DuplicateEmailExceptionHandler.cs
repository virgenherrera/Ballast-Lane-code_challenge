using Microsoft.AspNetCore.Diagnostics;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.API.Middleware;

/// <summary>
/// Project-wide reusable exception handler (.NET 8+ <see cref="IExceptionHandler"/>
/// pattern) that maps <see cref="DuplicateEmailException"/> — thrown when a
/// registration attempts to reuse an existing email — into the standard
/// 409 error response shape:
/// <code>
/// {
///   "status": 409,
///   "error": "CONFLICT",
///   "message": "An account with this email already exists.",
///   "details": []
/// }
/// </code>
/// Registered via <c>AddExceptionHandler&lt;DuplicateEmailExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c> in Program.cs.
/// </summary>
public sealed class DuplicateEmailExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not DuplicateEmailException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 409,
            error = "CONFLICT",
            message = "An account with this email already exists.",
            details = Array.Empty<object>(),
        }, ct);
        return true;
    }
}
