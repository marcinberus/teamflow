using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Commands.AssignMember;

public sealed class AssignMemberCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<AssignMemberCommandHandler> logger)
    : IRequestHandler<AssignMemberCommand, Result<AssignMemberResult>>
{
    public async Task<Result<AssignMemberResult>> Handle(
        AssignMemberCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithMembersAsync(
            request.ProjectId,
            cancellationToken);

        if (project is null)
        {
            logger.LogInformation("Project {ProjectId} not found.", request.ProjectId);
            return Result<AssignMemberResult>.Failure(ErrorMessages.NotFound);
        }

        Role? currentUserRole = Enum.TryParse<Role>(
            currentUserService.Role,
            out var parsedCurrentUserRole)
            && Enum.IsDefined(parsedCurrentUserRole)
                ? parsedCurrentUserRole
                : null;

        if (!project.CanAssignMembers(currentUserService.UserId, currentUserRole))
        {
            logger.LogWarning("User {CurrentUserId} is not allowed to assign members to project {ProjectId}.",
                currentUserService.UserId, request.ProjectId);
            return Result<AssignMemberResult>.Failure(ErrorMessages.Forbidden);
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            logger.LogInformation("User {UserId} was not found.", request.UserId);
            return Result<AssignMemberResult>.Failure(ErrorMessages.NotFound);
        }

        var role = Enum.Parse<Role>(request.ProjectRole);
        var member = project.AssignMember(user.Id, role, dateTimeProvider.UtcNow);

        await projectRepository.AddMemberAsync(member, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} was assigned to project {ProjectId}.",
            user.Id, project.Id);
        return Result<AssignMemberResult>.Success(new AssignMemberResult(member.Id));
    }
}
