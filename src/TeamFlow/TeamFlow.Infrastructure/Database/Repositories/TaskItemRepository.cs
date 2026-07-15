using TeamFlow.Application.Tasks.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Infrastructure.Database.Repositories;

public sealed class TaskItemRepository(TeamFlowDbContext context) : ITaskItemRepository
{
    public async Task AddAsync(TaskItem taskItem, CancellationToken cancellationToken)
    {
        await context.Tasks.AddAsync(taskItem, cancellationToken);
    }
}
