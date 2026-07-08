using Microsoft.EntityFrameworkCore;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Database;

public class TeamFlowDbContext : DbContext
{
    public TeamFlowDbContext(DbContextOptions<TeamFlowDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
