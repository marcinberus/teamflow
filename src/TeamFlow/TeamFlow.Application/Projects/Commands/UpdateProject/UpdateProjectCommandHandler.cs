using MediatR;
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
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateProjectCommand, Result<UpdateProjectResult>>
{
    public async Task<Result<UpdateProjectResult>> Handle(
        UpdateProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result<UpdateProjectResult>.Failure(ErrorMessages.NotFound);
        }

        var isAdmin = string.Equals(
            currentUserService.Role,
            nameof(Role.Admin),
            StringComparison.OrdinalIgnoreCase);

        if (project.OwnerId != currentUserService.UserId && !isAdmin)
        {
            return Result<UpdateProjectResult>.Failure(ErrorMessages.Forbidden);
        }

        project.Update(request.Name, request.Description, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpdateProjectResult>.Success(new());
    }
}
