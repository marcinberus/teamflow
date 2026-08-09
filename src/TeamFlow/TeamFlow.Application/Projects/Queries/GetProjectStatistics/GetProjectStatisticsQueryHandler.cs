using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Application.Projects.Queries.GetProjectStatistics;

public sealed class GetProjectStatisticsQueryHandler(
    IProjectReadService projectReadService,
    ILogger<GetProjectStatisticsQueryHandler> logger)
    : IRequestHandler<GetProjectStatisticsQuery, Result<ProjectStatisticsDto>>
{
    public async Task<Result<ProjectStatisticsDto>> Handle(
        GetProjectStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var statistics = await projectReadService.GetStatisticsAsync(request.ProjectId, cancellationToken);

        if (statistics is null)
        {
            logger.LogInformation("Statistics for project {ProjectId} were not found.", request.ProjectId);
            return Result<ProjectStatisticsDto>.Failure(ErrorMessages.NotFound);
        }

        logger.LogInformation("Statistics for project {ProjectId} were retrieved.", request.ProjectId);
        return Result<ProjectStatisticsDto>.Success(statistics);
    }
}
