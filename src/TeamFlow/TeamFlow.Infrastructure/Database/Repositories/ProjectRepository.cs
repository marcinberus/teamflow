using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Database.Repositories;

public sealed class ProjectRepository(TeamFlowDbContext context) : IProjectRepository
{
    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        await context.Projects.AddAsync(project, cancellationToken);
    }
}
