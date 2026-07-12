using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Projects.Interfaces;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);
}
