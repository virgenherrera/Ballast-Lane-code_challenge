using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.ValueObjects;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // DECISION: column naming = snake_case via explicit HasColumnName()
        // DECISION: Email/PasswordHash VOs stored as plain string via HasConversion
        // DECISION: Id generation = none — client-generated UUID v7 (ValueGeneratedNever)
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasConversion(
                email => email.Value,
                value => Email.Create(value))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasConversion(
                hash => hash.Value,
                value => PasswordHash.Create(value))
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Case-insensitive uniqueness per Decision #4 is enforced via a raw SQL
        // functional index (CREATE UNIQUE INDEX ... (LOWER(email))) added directly
        // inside the AddUsersTable migration's Up()/Down() methods. EF Core's fluent
        // API has no first-class support for functional indexes, and a plain
        // HasIndex(x => x.Email).IsUnique() here would only produce a case-SENSITIVE
        // B-tree index — do NOT add one.
    }
}
