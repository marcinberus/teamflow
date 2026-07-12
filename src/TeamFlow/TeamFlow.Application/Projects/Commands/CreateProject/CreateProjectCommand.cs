using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Projects.Commands.CreateProject;

public record CreateProjectCommand(string Name, string Description) : IRequest<Result<CreateProjectResult>>;

public record CreateProjectResult(Guid ProjectId);
