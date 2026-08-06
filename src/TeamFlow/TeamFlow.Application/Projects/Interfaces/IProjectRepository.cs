using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Projects.Interfaces;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task AddMemberAsync(ProjectMember member, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Project?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);
    Task DeleteAsync(Project project, CancellationToken cancellationToken);
}
