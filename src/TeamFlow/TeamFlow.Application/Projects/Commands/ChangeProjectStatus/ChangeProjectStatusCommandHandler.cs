using MediatR;
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
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ChangeProjectStatusCommand, Result<ChangeProjectStatusResult>>
{
    public async Task<Result<ChangeProjectStatusResult>> Handle(
        ChangeProjectStatusCommand request,
        CancellationToken cancellationToken)
    {
        var newStatus = Enum.Parse<ProjectStatus>(request.Status, ignoreCase: true);
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result<ChangeProjectStatusResult>.Failure(ErrorMessages.NotFound);
        }

        var isAdmin = string.Equals(
            currentUserService.Role,
            nameof(Role.Admin),
            StringComparison.OrdinalIgnoreCase);

        if (project.OwnerId != currentUserService.UserId && !isAdmin)
        {
            return Result<ChangeProjectStatusResult>.Failure(ErrorMessages.Forbidden);
        }

        project.ChangeStatus(newStatus, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ChangeProjectStatusResult>.Success(new());
    }
}
