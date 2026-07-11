using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;

namespace TaskFlow.IntegrationTests.Persistence;

public sealed class TaskRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:17.5")
        .WithDatabase("taskflow")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    private AppDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString());

        return new AppDbContext(optionsBuilder.Options);
    }

    [Fact]
    public async Task AddAsync_PersistsTask_PreservesClientGeneratedId()
    {
        await using var dbContext = CreateDbContext();
        var repository = new TaskRepository(dbContext);

        var task = TaskItem.Create("Buy groceries", null, null, Guid.NewGuid());
        var expectedId = task.Id;

        await repository.AddAsync(task, CancellationToken.None);

        await using var verifyContext = CreateDbContext();
        var persisted = await verifyContext.Tasks.SingleAsync(t => t.Id == expectedId);

        Assert.Equal(expectedId, persisted.Id);
    }

    [Fact]
    public async Task AddAsync_PersistsAndRetrieves_StatusEnumRoundTripsCorrectly()
    {
        await using var dbContext = CreateDbContext();
        var repository = new TaskRepository(dbContext);

        var task = TaskItem.Create("Write report", "Quarterly report", null, Guid.NewGuid());

        await repository.AddAsync(task, CancellationToken.None);

        await using var verifyContext = CreateDbContext();
        var persisted = await verifyContext.Tasks.SingleAsync(t => t.Id == task.Id);

        Assert.Equal(Domain.Enums.TaskStatus.Pending, persisted.Status);
    }
}
