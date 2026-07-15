using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProjectCommand, Result<DeleteProjectResult>>
{
    public async Task<Result<DeleteProjectResult>> Handle(
        DeleteProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result<DeleteProjectResult>.Failure(ErrorMessages.NotFound);
        }

        var isAdmin = string.Equals(
            currentUserService.Role,
            nameof(Role.Admin),
            StringComparison.OrdinalIgnoreCase);

        if (project.OwnerId != currentUserService.UserId && !isAdmin)
        {
            return Result<DeleteProjectResult>.Failure(ErrorMessages.Forbidden);
        }

        await projectRepository.DeleteAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<DeleteProjectResult>.Success(new());
    }
}
