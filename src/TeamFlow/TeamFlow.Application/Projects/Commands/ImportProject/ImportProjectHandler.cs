using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects;

namespace TeamFlow.Application.Projects.Commands.ImportProject;

public sealed class ImportProjectHandler(
    IProjectImportManager projectImportManager,
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ImportProjectCommand, Result<ImportProjectResult>>
{
    public async Task<Result<ImportProjectResult>> Handle(
        ImportProjectCommand request,
        CancellationToken cancellationToken)
    {
        if (!FileExtensionParser.TryParse(request.Extension, out var extension))
        {
            return Result<ImportProjectResult>.Failure(ErrorMessages.InvalidExtension);
        }

        var projectsIds = new List<Guid>();

        await foreach (var projectLine in projectImportManager.Import(
            extension,
            request.Stream,
            cancellationToken))
        {
            var project = Project.Create(
                projectLine.Name,
                projectLine.Description,
                currentUserService.UserId,
                dateTimeProvider.UtcNow);

            await projectRepository.AddAsync(project, cancellationToken);
            projectsIds.Add(project.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ImportProjectResult>.Success(new ImportProjectResult(projectsIds));
    }
}
