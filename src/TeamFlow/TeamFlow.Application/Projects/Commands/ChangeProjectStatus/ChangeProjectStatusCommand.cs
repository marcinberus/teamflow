using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Projects.Commands.ChangeProjectStatus;

public sealed record ChangeProjectStatusCommand(
    Guid ProjectId,
    string Status) : IRequest<Result<ChangeProjectStatusResult>>;

public sealed record ChangeProjectStatusResult;

public sealed record ChangeProjectStatusRequest(string Status);
