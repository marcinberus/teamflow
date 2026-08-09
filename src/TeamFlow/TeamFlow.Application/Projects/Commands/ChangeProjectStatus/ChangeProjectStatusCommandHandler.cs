using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Commands.ChangeProjectStatus;

public sealed class ChangeProjectStatusCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<ChangeProjectStatusCommandHandler> logger)
    : IRequestHandler<ChangeProjectStatusCommand, Result<ChangeProjectStatusResult>>
{
    public async Task<Result<ChangeProjectStatusResult>> Handle(
        ChangeProjectStatusCommand request,
        CancellationToken cancellationToken)
    {
        var newStatus = Enum.Parse<ProjectStatus>(request.Status);
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            logger.LogInformation("Project {ProjectId} was not found while changing its status.", request.ProjectId);
            return Result<ChangeProjectStatusResult>.Failure(ErrorMessages.NotFound);
        }

        var isAdmin = string.Equals(
            currentUserService.Role,
            nameof(Role.Admin),
            StringComparison.Ordinal);

        if (project.OwnerId != currentUserService.UserId && !isAdmin)
        {
            logger.LogWarning("User {UserId} is not allowed to change the status of project {ProjectId}.",
                currentUserService.UserId, project.Id);
            return Result<ChangeProjectStatusResult>.Failure(ErrorMessages.Forbidden);
        }

        project.ChangeStatus(newStatus, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Project {ProjectId} changed status to {Status}.", project.Id, newStatus);
        return Result<ChangeProjectStatusResult>.Success(new());
    }
}
