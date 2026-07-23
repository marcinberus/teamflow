using TeamFlow.Importing.FileExtensions;

namespace TeamFlow.Importing;

public interface IImporter
{
    bool CanImport(FileExtension fileExtension);
}
