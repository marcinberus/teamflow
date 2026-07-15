using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Tasks.Interfaces;

namespace TeamFlow.Application.Tasks.Queries.ListTasks;

public sealed class ListTasksQueryHandler(ITaskItemReadService taskItemReadService)
    : IRequestHandler<ListTasksQuery, Result<ListTasksResult>>
{
    public async Task<Result<ListTasksResult>> Handle(
        ListTasksQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await taskItemReadService.ListTasksAsync(
            request.ProjectId,
            request.Status,
            request.AssignedUserId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var result = new ListTasksResult(items, totalCount, request.Page, request.PageSize);

        return Result<ListTasksResult>.Success(result);
    }
}
