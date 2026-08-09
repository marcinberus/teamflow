using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Commands.RemoveMember;

public sealed class RemoveMemberCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    ILogger<RemoveMemberCommandHandler> logger)
    : IRequestHandler<RemoveMemberCommand, Result<RemoveMemberResult>>
{
    public async Task<Result<RemoveMemberResult>> Handle(
        RemoveMemberCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithMembersAsync(
            request.ProjectId,
            cancellationToken);

        if (project is null)
        {
            logger.LogInformation("Project {ProjectId} was not found while removing user {UserId}.",
                request.ProjectId, request.UserId);
            return Result<RemoveMemberResult>.Failure(ErrorMessages.NotFound);
        }

        Role? currentUserRole = Enum.TryParse<Role>(
            currentUserService.Role,
            out var parsedCurrentUserRole)
            && Enum.IsDefined(parsedCurrentUserRole)
                ? parsedCurrentUserRole
                : null;

        if (!project.CanAssignMembers(currentUserService.UserId, currentUserRole))
        {
            logger.LogWarning("User {CurrentUserId} is not allowed to remove members from project {ProjectId}.",
                currentUserService.UserId, project.Id);
            return Result<RemoveMemberResult>.Failure(ErrorMessages.Forbidden);
        }

        project.RemoveMember(request.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} was removed from project {ProjectId}.", request.UserId, project.Id);
        return Result<RemoveMemberResult>.Success(new());
    }
}
