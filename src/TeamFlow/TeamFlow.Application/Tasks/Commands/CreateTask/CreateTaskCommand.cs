using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    Guid ProjectId,
    string Title,
    string Description,
    Guid? AssignedUserId,
    DateTimeOffset? DueDate) : IRequest<Result<CreateTaskResult>>;

public sealed record CreateTaskResult(Guid TaskId);

public sealed record CreateTaskRequest(
    string Title,
    string Description,
    Guid? AssignedUserId,
    DateTimeOffset? DueDate);
