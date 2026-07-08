using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Database.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");

        builder.HasKey(pm => pm.Id);

        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique();

        builder.Property(pm => pm.ProjectId)
            .IsRequired();

        builder.Property(pm => pm.UserId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pm => pm.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(pm => pm.CreatedAt)
            .IsRequired();
    }
}
