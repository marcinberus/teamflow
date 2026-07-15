using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Projects.Interfaces;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteAsync(Project project, CancellationToken cancellationToken);
}
