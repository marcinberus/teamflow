using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Commands.RemoveMember;

public sealed class RemoveMemberCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
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
            return Result<RemoveMemberResult>.Failure(ErrorMessages.NotFound);
        }

        Role? currentUserRole = Enum.TryParse<Role>(
            currentUserService.Role,
            ignoreCase: true,
            out var parsedCurrentUserRole)
            && Enum.IsDefined(parsedCurrentUserRole)
                ? parsedCurrentUserRole
                : null;

        if (!project.CanAssignMembers(currentUserService.UserId, currentUserRole))
        {
            return Result<RemoveMemberResult>.Failure(ErrorMessages.Forbidden);
        }

        project.RemoveMember(request.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RemoveMemberResult>.Success(new());
    }
}
