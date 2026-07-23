using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Importing.Projects.Importerts;

public class JsonImporter : IProjectImporter
{
    public bool CanImport(FileExtension fileExtension) => fileExtension == FileExtension.Json;

    public IAsyncEnumerable<ProjectLine> Import(Stream stream, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
