using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Database.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(p => p.OwnerId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Members)
            .WithOne()
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tasks)
            .WithOne()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(p => p.Tasks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.CreatedAt)
            .IsRequired();
    }
}
