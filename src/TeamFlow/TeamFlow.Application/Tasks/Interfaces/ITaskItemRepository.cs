using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tasks.Interfaces;

public interface ITaskItemRepository
{
    Task AddAsync(TaskItem taskItem, CancellationToken cancellationToken);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
