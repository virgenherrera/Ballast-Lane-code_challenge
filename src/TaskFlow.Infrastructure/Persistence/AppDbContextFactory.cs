using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by <c>dotnet ef migrations add / database update</c>.
/// Not used at runtime — real DI wiring for <see cref="AppDbContext"/> happens in
/// TaskFlow.API's Program.cs (EP01-B1-04a). Kept minimal on purpose.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TASKFLOW_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=taskflow";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
