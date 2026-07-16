using MediatR;
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
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<ChangeTaskStatusCommand, Result<ChangeTaskStatusResult>>
{
    public async Task<Result<ChangeTaskStatusResult>> Handle(
        ChangeTaskStatusCommand request,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithMembersAsync(
            request.ProjectId,
            cancellationToken);

        if (project is null)
        {
            return Result<ChangeTaskStatusResult>.Failure(ErrorMessages.NotFound);
        }

        if (!project.HasMember(currentUserService.UserId))
        {
            return Result<ChangeTaskStatusResult>.Failure(ErrorMessages.Forbidden);
        }

        var taskItem = await taskItemRepository.GetByIdAsync(request.TaskId, cancellationToken);

        if (taskItem is null || taskItem.ProjectId != request.ProjectId)
        {
            return Result<ChangeTaskStatusResult>.Failure(ErrorMessages.NotFound);
        }

        var newStatus = Enum.Parse<TaskItemStatus>(request.Status, ignoreCase: true);
        taskItem.ChangeStatus(newStatus, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ChangeTaskStatusResult>.Success(new());
    }
}
