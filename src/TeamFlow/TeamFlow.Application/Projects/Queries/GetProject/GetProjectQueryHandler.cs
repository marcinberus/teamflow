using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Application.Projects.Queries.GetProject;

public sealed class GetProjectQueryHandler(
    IProjectReadService projectReadService,
    ILogger<GetProjectQueryHandler> logger)
    : IRequestHandler<GetProjectQuery, Result<ProjectDetailsDto>>
{
    public async Task<Result<ProjectDetailsDto>> Handle(
        GetProjectQuery request,
        CancellationToken cancellationToken)
    {
        var project = await projectReadService.GetProjectByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            logger.LogInformation("Project {ProjectId} was not found.", request.ProjectId);
            return Result<ProjectDetailsDto>.Failure(ErrorMessages.NotFound);
        }

        logger.LogInformation("Project {ProjectId} was retrieved.", request.ProjectId);
        return Result<ProjectDetailsDto>.Success(project);
    }
}
