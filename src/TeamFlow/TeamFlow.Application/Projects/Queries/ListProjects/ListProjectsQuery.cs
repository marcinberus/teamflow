using MediatR;

namespace TeamFlow.Application.Projects.Queries.ListProjects;

public record ListProjectsQuery(int Page = 1, int PageSize = 20, string? Status = null)
    : IRequest<ListProjectsResult>;
