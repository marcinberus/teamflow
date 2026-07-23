using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Projects.Commands.ImportProject;

public record ImportProjectCommand(Stream Stream, string Extension) : IRequest<Result<ImportProjectResult>>;

public record ImportProjectResult(IEnumerable<Guid> ProjectIds);
