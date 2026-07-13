using TeamFlow.Application.Projects.DTOs;

namespace TeamFlow.Application.Projects.Queries.ListProjects;

public record ListProjectsResult(
    IReadOnlyList<ProjectSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
