using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Tasks.Commands.ChangeTaskStatus;

public sealed record ChangeTaskStatusCommand(
    Guid ProjectId,
    Guid TaskId,
    string Status) : IRequest<Result<ChangeTaskStatusResult>>;

public sealed record ChangeTaskStatusResult;

public sealed record ChangeTaskStatusRequest(string Status);
