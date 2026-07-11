namespace TaskFlow.API.Configuration;

/// <summary>
/// Fail-fast validator for required environment variables. Must run before
/// <c>WebApplication.Build()</c> so a misconfigured environment never reaches
/// a listening state.
/// </summary>
public static class EnvVarValidator
{
    private static readonly string[] RequiredVariables =
    [
        "DB_HOST",
        "DB_PORT",
        "DB_USER",
        "DB_PASSWORD",
        "DB_NAME",
        "API_PORT",
        "JWT_SECRET",
        "JWT_ISSUER",
        "JWT_AUDIENCE",
    ];

    /// <summary>
    /// Reads and validates all required environment variables, returning them
    /// as a lookup keyed by variable name. Throws <see cref="InvalidOperationException"/>
    /// naming the first missing variable encountered.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ValidateAndRead()
    {
        var values = new Dictionary<string, string>();

        foreach (var variableName in RequiredVariables)
        {
            var value = Environment.GetEnvironmentVariable(variableName);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Missing required environment variable: {variableName}");
            }

            values[variableName] = value;
        }

        return values;
    }
}
