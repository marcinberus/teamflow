using MediatR;
using TeamFlow.Application.Common;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.Interfaces;
using TeamFlow.Application.Tasks.Interfaces;

namespace TeamFlow.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler(
    ICurrentUserService currentUserService,
    IProjectRepository projectRepository,
    ITaskItemRepository taskItemRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<UpdateTaskCommand, Result<UpdateTaskResult>>
{
    public async Task<Result<UpdateTaskResult>> Handle(
        UpdateTaskCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithMembersAsync(
            request.ProjectId,
            cancellationToken);

        if (project is null)
        {
            return Result<UpdateTaskResult>.Failure(ErrorMessages.NotFound);
        }

        if (!project.HasMember(currentUserService.UserId))
        {
            return Result<UpdateTaskResult>.Failure(ErrorMessages.Forbidden);
        }

        var taskItem = await taskItemRepository.GetByIdAsync(request.TaskId, cancellationToken);

        if (taskItem is null || taskItem.ProjectId != request.ProjectId)
        {
            return Result<UpdateTaskResult>.Failure(ErrorMessages.NotFound);
        }

        //if (request.AssignedUserId.HasValue            && !project.HasMember(request.AssignedUserId.Value))
        if (request.AssignedUserId is { } assignedUserId && !project.HasMember(assignedUserId))
        {
            return Result<UpdateTaskResult>.Failure(ErrorMessages.AssignedUserNotProjectMember);
        }

        taskItem.Update(
            request.Title,
            request.Description,
            request.AssignedUserId,
            request.DueDate,
            dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpdateTaskResult>.Success(new());
    }
}
