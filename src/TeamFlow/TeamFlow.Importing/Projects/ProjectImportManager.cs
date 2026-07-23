using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Importing.Projects;

public sealed class ProjectImportManager(IEnumerable<IProjectImporter> projectImporters) : IProjectImportManager
{
    public IAsyncEnumerable<ProjectLine> Import(
        FileExtension fileExtension,
        Stream stream,
        CancellationToken cancellationToken)
    {
        var importer = projectImporters
            .SingleOrDefault(importer => importer.CanImport(fileExtension));

        if (importer == null)
        {
            return EmptyProjectLines();
        }

        return importer.Import(stream, cancellationToken);
    }

    private static async IAsyncEnumerable<ProjectLine> EmptyProjectLines()
    {
        await Task.CompletedTask;

        yield break;
    }
}
