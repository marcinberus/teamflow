using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string Description) : IRequest<Result<UpdateProjectResult>>;

public sealed record UpdateProjectResult;

public sealed record UpdateProjectRequest(string Name, string Description);
