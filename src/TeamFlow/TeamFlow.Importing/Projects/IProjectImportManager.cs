using TeamFlow.Domain.Entities;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Importing.Projects;

public interface IProjectImportManager
{
    IAsyncEnumerable<ProjectLine> Import(
        FileExtension fileExtension,
        Stream stream,
        CancellationToken cancellationToken);
}
