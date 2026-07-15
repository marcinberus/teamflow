using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest<Result<DeleteProjectResult>>;

public sealed record DeleteProjectResult;
