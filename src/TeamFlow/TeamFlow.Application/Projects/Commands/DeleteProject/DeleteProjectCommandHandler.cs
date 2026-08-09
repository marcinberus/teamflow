using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteProjectCommandHandler> logger)
    : IRequestHandler<DeleteProjectCommand, Result<DeleteProjectResult>>
{
    public async Task<Result<DeleteProjectResult>> Handle(
        DeleteProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            logger.LogInformation("Project {ProjectId} was not found while deleting it.", request.ProjectId);
            return Result<DeleteProjectResult>.Failure(ErrorMessages.NotFound);
        }

        var isAdmin = string.Equals(
            currentUserService.Role,
            nameof(Role.Admin),
            StringComparison.Ordinal);

        if (project.OwnerId != currentUserService.UserId && !isAdmin)
        {
            logger.LogWarning("User {UserId} is not allowed to delete project {ProjectId}.",
                currentUserService.UserId, project.Id);
            return Result<DeleteProjectResult>.Failure(ErrorMessages.Forbidden);
        }

        await projectRepository.DeleteAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Project {ProjectId} was deleted by user {UserId}.", project.Id, currentUserService.UserId);
        return Result<DeleteProjectResult>.Success(new());
    }
}
