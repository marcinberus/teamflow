using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Importing.TaskItems;

public class TaskItemImportManager(IEnumerable<ITaskItemImporter> taskItemImporters) 
    : ImportManager<TaskItemLine>(taskItemImporters), IImportManager<TaskItemLine>
{
}
