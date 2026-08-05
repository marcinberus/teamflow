using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Importing;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Application.Tasks.Commands.ImportTask;

public class ImportTaskItemHandler(
    IImportManager<TaskItemLine> importManager,
    ICurrentUserService currentUserService,
    ITaskItemRepository taskItemRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ImportTaskItemCommand, Result<ImportTaskItemResult>>
{
    public async Task<Result<ImportTaskItemResult>> Handle(ImportTaskItemCommand request, CancellationToken cancellationToken)
    {
        if (!FileExtensionParser.TryParse(request.Extension, out var extension))
        {
            return Result<ImportTaskItemResult>.Failure(ErrorMessages.InvalidExtension);
        }

        var tasksIds = new List<Guid>();

        await foreach (var taskItemLine in importManager.Import(
            extension,
            request.Stream,
            cancellationToken))
        {
            // TODO: status from file
            var taskItem = TaskItem.Create(
                request.ProjectId,
                taskItemLine.Title.ToString(),
                taskItemLine.Description.ToString(),
                // TODO: assigned user from file
                currentUserService.UserId,
                // TODO: due date from file
                null,
                dateTimeProvider.UtcNow);

            await taskItemRepository.AddAsync(taskItem, cancellationToken);
            tasksIds.Add(taskItem.Id);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ImportTaskItemResult>.Success(new ImportTaskItemResult(tasksIds));
    }
}
