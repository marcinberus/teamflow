using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tasks.Commands.ChangeTaskStatus;

public sealed class ChangeTaskStatusCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    ITaskItemRepository taskItemRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<ChangeTaskStatusCommandHandler> logger)
    : IRequestHandler<ChangeTaskStatusCommand, Result<ChangeTaskStatusResult>>
{
    public async Task<Result<ChangeTaskStatusResult>> Handle(
        ChangeTaskStatusCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(
            request.ProjectId,
            cancellationToken);

        if (project is null)
        {
            logger.LogInformation("Project {ProjectId} was not found while changing task {TaskId} status.",
                request.ProjectId, request.TaskId);
            return Result<ChangeTaskStatusResult>.Failure(ErrorMessages.NotFound);
        }

        if (!await projectRepository.HasMemberAsync(
                project.Id,
                currentUserService.UserId,
                cancellationToken))
        {
            logger.LogWarning("User {UserId} is not allowed to change task status in project {ProjectId}.",
                currentUserService.UserId, project.Id);
            return Result<ChangeTaskStatusResult>.Failure(ErrorMessages.Forbidden);
        }

        var taskItem = await taskItemRepository.GetByIdAsync(request.TaskId, cancellationToken);

        if (taskItem is null || taskItem.ProjectId != request.ProjectId)
        {
            logger.LogInformation("Task {TaskId} was not found in project {ProjectId}.", request.TaskId, request.ProjectId);
            return Result<ChangeTaskStatusResult>.Failure(ErrorMessages.NotFound);
        }

        var newStatus = Enum.Parse<TaskItemStatus>(request.Status);
        taskItem.ChangeStatus(newStatus, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Task {TaskId} in project {ProjectId} changed status to {Status}.",
            taskItem.Id, project.Id, newStatus);
        return Result<ChangeTaskStatusResult>.Success(new());
    }
}
