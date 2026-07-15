using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Database.Repositories;

public sealed class ProjectRepository(TeamFlowDbContext context) : IProjectRepository
{
    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        await context.Projects.AddAsync(project, cancellationToken);
    }

    public async Task AddMemberAsync(ProjectMember member, CancellationToken cancellationToken)
    {
        await context.ProjectMembers.AddAsync(member, cancellationToken);
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Projects.FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

    public Task<Project?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken) =>
        context.Projects
            .Include(project => project.Members)
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

    public Task DeleteAsync(Project project, CancellationToken cancellationToken)
    {
        context.Projects.Remove(project);

        return Task.CompletedTask;
    }
}
