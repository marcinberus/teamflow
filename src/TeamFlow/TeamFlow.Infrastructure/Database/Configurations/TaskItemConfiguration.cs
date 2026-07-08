using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Database.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.ProjectId)
            .IsRequired();

        builder.Property(t => t.AssignedUserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.AssignedUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
