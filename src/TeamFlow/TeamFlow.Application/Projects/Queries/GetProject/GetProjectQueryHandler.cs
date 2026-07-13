using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.DTOs;
using TeamFlow.Application.Projects.Interfaces;

namespace TeamFlow.Application.Projects.Queries.GetProject;

public sealed class GetProjectQueryHandler(IProjectReadService projectReadService)
    : IRequestHandler<GetProjectQuery, Result<ProjectDetailsDto>>
{
    public async Task<Result<ProjectDetailsDto>> Handle(
        GetProjectQuery request,
        CancellationToken cancellationToken)
    {
        var project = await projectReadService.GetProjectByIdAsync(request.ProjectId, cancellationToken);

        return project is null
            ? Result<ProjectDetailsDto>.Failure(ErrorMessages.NotFound)
            : Result<ProjectDetailsDto>.Success(project);
    }
}
