using TeamFlow.Application.Projects.DTOs;

namespace TeamFlow.Application.Projects.Interfaces;

public interface IProjectReadService
{
    Task<ProjectDetailsDto?> GetProjectByIdAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<ProjectSummaryDto> Items, int TotalCount)> ListProjectsAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectMemberDto>> ListMembersAsync(
        Guid projectId,
        CancellationToken cancellationToken);
}
