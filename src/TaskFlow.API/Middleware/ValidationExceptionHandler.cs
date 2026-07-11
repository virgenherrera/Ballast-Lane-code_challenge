using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace TaskFlow.API.Middleware;

/// <summary>
/// Project-wide reusable exception handler (.NET 8+ <see cref="IExceptionHandler"/>
/// pattern) that maps <see cref="ValidationException"/> — thrown by any
/// controller after running a FluentValidation validator — into the
/// standard error response shape:
/// <code>
/// {
///   "status": 400,
///   "error": "VALIDATION_ERROR",
///   "message": "One or more validation errors occurred.",
///   "details": [ { "field": "title", "issue": "title required" } ]
/// }
/// </code>
/// Registered via <c>AddExceptionHandler&lt;ValidationExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c> in Program.cs. Not specific to any single
/// controller — any endpoint that throws <see cref="ValidationException"/>
/// gets this shape for free.
/// </summary>
public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/json";

        var details = validationException.Errors.Select(e => new
        {
            field = ToCamelCase(e.PropertyName),
            issue = e.ErrorMessage,
        });

        var body = new
        {
            status = StatusCodes.Status400BadRequest,
            error = "VALIDATION_ERROR",
            message = "One or more validation errors occurred.",
            details,
        };

        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);

        return true;
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return string.Concat(char.ToLowerInvariant(value[0]).ToString(), value.AsSpan(1));
    }
}
