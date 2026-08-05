using TeamFlow.Importing.FileExtensions;

namespace TeamFlow.Importing;

public class ImportManager<T>(IEnumerable<IImporter<T>> importers) where T : IImportLine
{
    public IAsyncEnumerable<T> Import(
        FileExtension fileExtension,
        Stream stream,
        CancellationToken cancellationToken)
    {
        var importer = importers
            .SingleOrDefault(importer => importer.CanImport(fileExtension));

        if (importer == null)
        {
            return EmptyLines();
        }

        return importer.Import(stream, cancellationToken);
    }

    private static async IAsyncEnumerable<T> EmptyLines()
    {
        await Task.CompletedTask;

        yield break;
    }
}
