using TeamFlow.Application.Projects.DTOs;

namespace TeamFlow.Application.Projects.Interfaces;

public interface IProjectReadService
{
    Task<(IReadOnlyList<ProjectSummaryDto> Items, int TotalCount)> ListProjectsAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken);
}
