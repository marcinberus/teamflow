using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Application.Users.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Importing;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Application.Tasks.Commands.ImportTask;

public class ImportTaskItemHandler(
    IImportManager<TaskItemLine> importManager,
    ICurrentUserService currentUserService,
    ITaskItemRepository taskItemRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<ImportTaskItemHandler> logger) : IRequestHandler<ImportTaskItemCommand, Result<ImportTaskItemResult>>
{
    public async Task<Result<ImportTaskItemResult>> Handle(ImportTaskItemCommand request, CancellationToken cancellationToken)
    {
        if (!FileExtensionParser.TryParse(request.Extension, out var extension))
        {
            logger.LogWarning("Task import for project {ProjectId} was rejected because extension {Extension} is invalid.",
                request.ProjectId, request.Extension);
            return Result<ImportTaskItemResult>.Failure(ErrorMessages.InvalidExtension);
        }

        var tasksIds = new List<Guid>();

        await foreach (var taskItemLine in importManager.Import(
            extension,
            request.Stream,
            cancellationToken))
        {
            if (!Enum.TryParse<TaskItemStatus>(taskItemLine.Status.Span, ignoreCase: true, out var status)
                || !Enum.IsDefined(status))
            {
                status = TaskItemStatus.Todo;
            }

            var isExistingUser = false;
            if (!Guid.TryParse(taskItemLine.UserId.ToString(), out var userId))
            {
                userId = currentUserService.UserId;
                isExistingUser = true;
            }

            if (!isExistingUser && !(await userRepository.ExistsByUserIdAsync(userId, cancellationToken)))
            {
                userId = currentUserService.UserId;
            }

            var taskItem = TaskItem.Create(
                request.ProjectId,
                taskItemLine.Title.ToString(),
                taskItemLine.Description.ToString(),
                userId,
                // TODO: due date from file
                null,
                dateTimeProvider.UtcNow,
                status);

            await taskItemRepository.AddAsync(taskItem, cancellationToken);
            tasksIds.Add(taskItem.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Imported {TaskCount} tasks into project {ProjectId}.", tasksIds.Count, request.ProjectId);
        return Result<ImportTaskItemResult>.Success(new ImportTaskItemResult(tasksIds));
    }
}
