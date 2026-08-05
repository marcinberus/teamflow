using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Importing.Projects;

public sealed class ProjectImportManager(IEnumerable<IProjectImporter> projectImporters) 
    : ImportManager<ProjectLine>(projectImporters), IImportManager<ProjectLine>
{
}
