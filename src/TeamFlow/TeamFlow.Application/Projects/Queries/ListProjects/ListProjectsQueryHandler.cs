using MediatR;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Application.Projects.Queries.ListProjects;

public sealed class ListProjectsQueryHandler(IProjectReadService projectReadService)
    : IRequestHandler<ListProjectsQuery, ListProjectsResult>
{
    public async Task<ListProjectsResult> Handle(
        ListProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await projectReadService.ListProjectsAsync(
            request.Page,
            request.PageSize,
            request.Status,
            cancellationToken);

        return new ListProjectsResult(items, totalCount, request.Page, request.PageSize);
    }
}
