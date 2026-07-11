using Microsoft.AspNetCore.Diagnostics;
using TaskFlow.Application.Common.Exceptions;

namespace TaskFlow.API.Middleware;

/// <summary>
/// Project-wide reusable exception handler (.NET 8+ <see cref="IExceptionHandler"/>
/// pattern) that maps <see cref="TaskNotFoundException"/> — thrown when a
/// task does not exist or is owned by another user — into the standard
/// 404 error response shape:
/// <code>
/// {
///   "status": 404,
///   "error": "NOT_FOUND",
///   "message": "The requested task was not found.",
///   "details": []
/// }
/// </code>
/// Registered via <c>AddExceptionHandler&lt;TaskNotFoundExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c> in Program.cs.
/// </summary>
public sealed class TaskNotFoundExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not TaskNotFoundException)
            return false;

        httpContext.Response.StatusCode = 404;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 404,
            error = "NOT_FOUND",
            message = "The requested task was not found.",
            details = Array.Empty<object>()
        }, ct);
        return true;
    }
}
