using MediatR;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Tasks.Commands.ImportTask;

public record ImportTaskItemCommand(Guid ProjectId, Stream Stream, string Extension) : IRequest<Result<ImportTaskItemResult>>;

public record ImportTaskItemResult(IEnumerable<Guid> TaskItemsIds);