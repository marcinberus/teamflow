using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Application.Projects.Queries.ListProjects;

public sealed class ListProjectsQueryHandler(
    IProjectReadService projectReadService,
    ILogger<ListProjectsQueryHandler> logger)
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

        logger.LogInformation(
            "Retrieved {ProjectCount} projects on page {Page} with page size {PageSize}.",
            items.Count,
            request.Page,
            request.PageSize);
        return new ListProjectsResult(items, totalCount, request.Page, request.PageSize);
    }
}
