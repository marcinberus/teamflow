using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand(
    Guid ProjectId,
    Guid TaskId,
    string Title,
    string Description,
    Guid? AssignedUserId,
    DateTimeOffset? DueDate) : IRequest<Result<UpdateTaskResult>>;

public sealed record UpdateTaskResult;

public sealed record UpdateTaskRequest(
    string Title,
    string Description,
    Guid? AssignedUserId,
    DateTimeOffset? DueDate);
