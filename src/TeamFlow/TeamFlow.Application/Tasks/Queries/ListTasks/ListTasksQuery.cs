using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Tasks.Queries.ListTasks;

public record ListTasksQuery(
    Guid ProjectId,
    string? Status = null,
    Guid? AssignedUserId = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<ListTasksResult>>;
