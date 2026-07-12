using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>
/// Seeds a demo user and sample tasks on startup. Idempotent — safe to run
/// on every application boot (e.g. inside Docker), since it no-ops once the
/// demo user already exists.
/// </summary>
public static class DbSeeder
{
    private const string DemoEmail = "demo@taskflow.dev";
    private const string DemoPassword = "Demo123!";
    private const string DemoName = "Demo User";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var email = Email.Create(DemoEmail);

        var alreadySeeded = await dbContext.Users
            .AnyAsync(u => u.Email == email);

        if (alreadySeeded)
        {
            return;
        }

        var demoUser = User.Create(email, DemoName, passwordHasher.Hash(DemoPassword));

        dbContext.Users.Add(demoUser);
        await dbContext.SaveChangesAsync();

        var task1 = TaskItem.Create(
            "Review project documentation",
            "Read through the technical brief and identify key deliverables",
            DateTime.UtcNow.AddDays(2),
            demoUser.Id);

        var task2 = TaskItem.Create(
            "Set up CI/CD pipeline",
            "Configure GitHub Actions for automated testing and deployment",
            DateTime.UtcNow.AddDays(5),
            demoUser.Id);
        task2.ChangeStatus(Domain.Enums.TaskStatus.InProgress);

        var task3 = TaskItem.Create(
            "Write API integration tests",
            "Cover all CRUD endpoints with real database tests",
            null,
            demoUser.Id);
        task3.ChangeStatus(Domain.Enums.TaskStatus.InProgress);
        task3.ChangeStatus(Domain.Enums.TaskStatus.Completed);

        var task4 = TaskItem.Create(
            "Design frontend components",
            "Create reusable UI components following atomic design principles",
            DateTime.UtcNow.AddDays(7),
            demoUser.Id);

        var task5 = TaskItem.Create(
            "Implement user authentication",
            "Add JWT-based login and registration flows",
            null,
            demoUser.Id);
        task5.ChangeStatus(Domain.Enums.TaskStatus.InProgress);
        task5.ChangeStatus(Domain.Enums.TaskStatus.Completed);

        dbContext.Tasks.AddRange(task1, task2, task3, task4, task5);
        await dbContext.SaveChangesAsync();
    }
}
