using MediatR;
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
    IDateTimeProvider dateTimeProvider)
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
            return Result<AssignMemberResult>.Failure(ErrorMessages.Forbidden);
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<AssignMemberResult>.Failure(ErrorMessages.NotFound);
        }

        var role = Enum.Parse<Role>(request.ProjectRole);
        project.AssignMember(user.Id, role, dateTimeProvider.UtcNow);
        var member = project.Members.Single(item => item.UserId == user.Id);

        await projectRepository.AddMemberAsync(member, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AssignMemberResult>.Success(new AssignMemberResult(member.Id));
    }
}
