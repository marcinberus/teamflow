using TeamFlow.Application.Tasks.DTOs;

namespace TeamFlow.Application.Tasks.Interfaces;

public interface ITaskItemReadService
{
    Task<(IReadOnlyList<TaskItemDto> Items, int TotalCount)> ListTasksAsync(
        Guid projectId,
        string? status,
        Guid? assignedUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
