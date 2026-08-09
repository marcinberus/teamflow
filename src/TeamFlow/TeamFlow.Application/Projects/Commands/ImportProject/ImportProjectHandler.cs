using MediatR;
using TeamFlow.Application.Common;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Importing;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Application.Projects.Commands.ImportProject;

public sealed class ImportProjectHandler(
    IImportManager<ProjectLine> importManager,
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<ImportProjectHandler> logger) : IRequestHandler<ImportProjectCommand, Result<ImportProjectResult>>
{
    public async Task<Result<ImportProjectResult>> Handle(
        ImportProjectCommand request,
        CancellationToken cancellationToken)
    {
        if (!FileExtensionParser.TryParse(request.Extension, out var extension))
        {
            logger.LogWarning("Project import was rejected because extension {Extension} is invalid.", request.Extension);
            return Result<ImportProjectResult>.Failure(ErrorMessages.InvalidExtension);
        }

        var projectsIds = new List<Guid>();

        await foreach (var projectLine in importManager.Import(
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

        logger.LogInformation("User {UserId} imported {ProjectCount} projects.",
            currentUserService.UserId, projectsIds.Count);
        return Result<ImportProjectResult>.Success(new ImportProjectResult(projectsIds));
    }
}
