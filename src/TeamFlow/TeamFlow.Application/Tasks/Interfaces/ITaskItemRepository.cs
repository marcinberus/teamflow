using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tasks.Interfaces;

public interface ITaskItemRepository
{
    Task AddAsync(TaskItem taskItem, CancellationToken cancellationToken);
}
