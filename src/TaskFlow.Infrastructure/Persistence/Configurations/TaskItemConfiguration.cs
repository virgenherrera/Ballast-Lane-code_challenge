using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Constants;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        // DECISION: column naming = snake_case via explicit HasColumnName()
        // DECISION: TaskStatus stored as smallint via HasConversion<int>()
        builder.ToTable("tasks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(FieldLengths.TitleMaxLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(FieldLengths.DescriptionMaxLength);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>();

        builder.Property(x => x.DueDate)
            .HasColumnName("due_date");

        builder.Property(x => x.OwnerId)
            .HasColumnName("owner_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
