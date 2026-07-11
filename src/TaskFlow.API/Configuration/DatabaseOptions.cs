namespace TaskFlow.API.Configuration;

/// <summary>
/// Bound from the "Database" configuration section. Populated in
/// <c>Program.cs</c> from environment variables — never read directly
/// from <c>IConfiguration</c> in handlers.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;
}
