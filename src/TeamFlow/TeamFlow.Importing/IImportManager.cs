using TeamFlow.Importing.FileExtensions;

namespace TeamFlow.Importing;

public interface IImportManager<T> where T : IImportLine
{
    IAsyncEnumerable<T> Import(
    FileExtension fileExtension,
    Stream stream,
    CancellationToken cancellationToken);
}
