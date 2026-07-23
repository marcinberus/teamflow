using TeamFlow.Domain.Entities;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Importing.Projects;

public interface IProjectImporter : IImporter
{
    IAsyncEnumerable<ProjectLine> Import(Stream stream, CancellationToken cancellationToken);
}
