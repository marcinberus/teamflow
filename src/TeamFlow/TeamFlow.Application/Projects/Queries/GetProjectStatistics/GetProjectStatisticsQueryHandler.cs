using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Application.Projects.Queries.GetProjectStatistics;

public sealed class GetProjectStatisticsQueryHandler(IProjectReadService projectReadService)
    : IRequestHandler<GetProjectStatisticsQuery, Result<ProjectStatisticsDto>>
{
    public async Task<Result<ProjectStatisticsDto>> Handle(
        GetProjectStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var statistics = await projectReadService.GetStatisticsAsync(request.ProjectId, cancellationToken);

        return statistics is null
            ? Result<ProjectStatisticsDto>.Failure(ErrorMessages.NotFound)
            : Result<ProjectStatisticsDto>.Success(statistics);
    }
}
