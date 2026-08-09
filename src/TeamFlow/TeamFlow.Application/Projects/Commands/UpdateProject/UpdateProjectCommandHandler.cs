using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<UpdateProjectCommandHandler> logger)
    : IRequestHandler<UpdateProjectCommand, Result<UpdateProjectResult>>
{
    public async Task<Result<UpdateProjectResult>> Handle(
        UpdateProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            logger.LogInformation("Project {ProjectId} was not found while updating it.", request.ProjectId);
            return Result<UpdateProjectResult>.Failure(ErrorMessages.NotFound);
        }

        var isAdmin = string.Equals(
            currentUserService.Role,
            nameof(Role.Admin),
            StringComparison.Ordinal);

        if (project.OwnerId != currentUserService.UserId && !isAdmin)
        {
            logger.LogWarning("User {UserId} is not allowed to update project {ProjectId}.",
                currentUserService.UserId, project.Id);
            return Result<UpdateProjectResult>.Failure(ErrorMessages.Forbidden);
        }

        project.Update(request.Name, request.Description, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Project {ProjectId} was updated.", project.Id);
        return Result<UpdateProjectResult>.Success(new());
    }
}
