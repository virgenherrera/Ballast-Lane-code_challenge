using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.ValueObjects;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;

namespace TaskFlow.IntegrationTests.Persistence;

public sealed class UserRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:17.5")
        .WithDatabase("taskflow")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // MigrateAsync (not EnsureCreatedAsync) is required here: the
        // case-insensitive `LOWER(email)` unique index is added via raw SQL
        // inside AddUsersTable's migration Up(), not via the EF Core model,
        // so EnsureCreatedAsync (which builds schema straight from the model
        // snapshot) would silently skip it.
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
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

    private static User CreateUser(string email = "alice@example.com", string name = "Alice")
    {
        return User.Create(
            Email.Create(email),
            name,
            PasswordHash.Create("hashed-password-value"));
    }

    [Fact]
    public async Task AddAsync_PersistsUser_PreservesClientGeneratedId()
    {
        await using var dbContext = CreateDbContext();
        var repository = new UserRepository(dbContext);

        var user = CreateUser();
        var expectedId = user.Id;

        await repository.AddAsync(user, CancellationToken.None);

        await using var verifyContext = CreateDbContext();
        var persisted = await verifyContext.Users.SingleAsync(u => u.Id == expectedId);

        Assert.Equal(expectedId, persisted.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        var repository = new UserRepository(dbContext);

        var user = CreateUser("bob@example.com");
        await repository.AddAsync(user, CancellationToken.None);

        await using var queryContext = CreateDbContext();
        var queryRepository = new UserRepository(queryContext);

        var result = await queryRepository.GetByEmailAsync(Email.Create("bob@example.com"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistentEmail_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var repository = new UserRepository(dbContext);

        var result = await repository.GetByEmailAsync(Email.Create("nobody@example.com"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsUser()
    {
        await using var dbContext = CreateDbContext();
        var repository = new UserRepository(dbContext);

        var user = CreateUser("carol@example.com");
        await repository.AddAsync(user, CancellationToken.None);

        await using var queryContext = CreateDbContext();
        var queryRepository = new UserRepository(queryContext);

        var result = await queryRepository.GetByIdAsync(user.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var repository = new UserRepository(dbContext);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_EmailAlreadyRegistered_ReturnsTrue()
    {
        await using var dbContext = CreateDbContext();
        var repository = new UserRepository(dbContext);

        var user = CreateUser("dave@example.com");
        await repository.AddAsync(user, CancellationToken.None);

        await using var queryContext = CreateDbContext();
        var queryRepository = new UserRepository(queryContext);

        var exists = await queryRepository.ExistsAsync(Email.Create("dave@example.com"), CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_EmailNotRegistered_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();
        var repository = new UserRepository(dbContext);

        var exists = await repository.ExistsAsync(Email.Create("ghost@example.com"), CancellationToken.None);

        Assert.False(exists);
    }

    // NOTE: The Expected Test #8 from the handoff
    // (`AddAsync_DuplicateEmailDifferentCasing_ThrowsOnUniqueConstraintViolation`) is not
    // applicable exactly as named: `Email.Create` (Decision #4) rejects any uppercase input
    // at the Domain VO boundary, so it is impossible to construct two `Email` instances that
    // differ only by casing through the repository's public API — there is no way to reach
    // the DB with mixed-case duplicates from this layer. This test instead proves the
    // case-insensitive unique index (`LOWER(email)`, added in EP02-B2-01's migration) rejects
    // an exact-duplicate lowercase email, which is the reachable subset of that guarantee at
    // the repository layer. The uppercase-rejection half of Decision #4 is already covered by
    // `Email` VO unit tests, not here.
    [Fact]
    public async Task AddAsync_DuplicateEmail_ThrowsOnUniqueConstraintViolation()
    {
        await using var dbContext = CreateDbContext();
        var repository = new UserRepository(dbContext);

        var first = CreateUser("duplicate@example.com", "First User");
        await repository.AddAsync(first, CancellationToken.None);

        await using var secondContext = CreateDbContext();
        var secondRepository = new UserRepository(secondContext);
        var second = CreateUser("duplicate@example.com", "Second User");

        await Assert.ThrowsAsync<DbUpdateException>(
            () => secondRepository.AddAsync(second, CancellationToken.None));
    }
}
