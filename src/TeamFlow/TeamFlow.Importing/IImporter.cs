using TeamFlow.Importing.FileExtensions;

namespace TeamFlow.Importing;

public interface IImporter<T> where T : IImportLine
{
    bool CanImport(FileExtension fileExtension);
    IAsyncEnumerable<T> Import(Stream stream, CancellationToken cancellationToken);
}
