using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Tasks.Interfaces;

namespace TeamFlow.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    ITaskItemRepository taskItemRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<CreateTaskCommandHandler> logger)
    : IRequestHandler<CreateTaskCommand, Result<CreateTaskResult>>
{
    public async Task<Result<CreateTaskResult>> Handle(
        CreateTaskCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(
            request.ProjectId,
            cancellationToken);

        if (project is null)
        {
            logger.LogInformation("Project {ProjectId} was not found while creating a task.", request.ProjectId);
            return Result<CreateTaskResult>.Failure(ErrorMessages.NotFound);
        }

        var currentUserId = currentUserService.UserId;

        if (!await projectRepository.HasMemberAsync(
                project.Id,
                currentUserId,
                cancellationToken))
        {
            logger.LogWarning("User {UserId} is not allowed to create tasks in project {ProjectId}.",
                currentUserId, project.Id);
            return Result<CreateTaskResult>.Failure(ErrorMessages.Forbidden);
        }

        var assignedUserId = request.AssignedUserId ?? currentUserId;

        if (assignedUserId != currentUserId
            && !await projectRepository.HasMemberAsync(
                project.Id,
                assignedUserId,
                cancellationToken))
        {
            logger.LogInformation("User {AssignedUserId} is not a member of project {ProjectId} and cannot be assigned a task.",
                assignedUserId, project.Id);
            return Result<CreateTaskResult>.Failure(ErrorMessages.AssignedUserNotProjectMember);
        }

        var task = project.AddTask(
            request.Title,
            request.Description,
            assignedUserId,
            request.DueDate,
            dateTimeProvider.UtcNow);

        await taskItemRepository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Task {TaskId} was created in project {ProjectId} and assigned to user {AssignedUserId}.",
            task.Id, project.Id, assignedUserId);
        return Result<CreateTaskResult>.Success(new CreateTaskResult(task.Id));
    }
}
