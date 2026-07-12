using Microsoft.AspNetCore.Diagnostics;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.API.Middleware;

/// <summary>
/// Project-wide reusable exception handler (.NET 8+ <see cref="IExceptionHandler"/>
/// pattern) that maps <see cref="InvalidCredentialsException"/> — thrown when login
/// fails due to an unknown email or a wrong password — into the standard 401 error
/// response shape:
/// <code>
/// {
///   "status": 401,
///   "error": "UNAUTHORIZED",
///   "message": "Invalid email or password.",
///   "details": []
/// }
/// </code>
/// Registered via <c>AddExceptionHandler&lt;InvalidCredentialsExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c> in Program.cs.
/// </summary>
public sealed class InvalidCredentialsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not InvalidCredentialsException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 401,
            error = "UNAUTHORIZED",
            message = "Invalid email or password.",
            details = Array.Empty<object>(),
        }, ct);
        return true;
    }
}
