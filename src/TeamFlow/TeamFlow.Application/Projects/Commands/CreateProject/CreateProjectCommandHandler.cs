using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateProjectCommand, Result<CreateProjectResult>>
{
    public async Task<Result<CreateProjectResult>> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        var project = Project.Create(
            request.Name,
            request.Description,
            currentUserService.UserId,
            dateTimeProvider.UtcNow);

        await projectRepository.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateProjectResult>.Success(new CreateProjectResult(project.Id));
    }
}
