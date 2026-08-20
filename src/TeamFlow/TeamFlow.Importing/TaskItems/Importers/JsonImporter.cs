using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeamFlow.Importing.Common;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.TaskItems.Models;

namespace TeamFlow.Importing.TaskItems.Importers;

public class JsonImporter : ITaskItemImporter
{
    public bool CanImport(FileExtension fileExtension) => fileExtension == FileExtension.Json;

    public async IAsyncEnumerable<TaskItemLine> Import(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException(ErrorMessages.StreamUnreadable);
        }

        await foreach (var line in JsonSerializer.DeserializeAsyncEnumerable(
            stream,
            TaskItemJsonContext.Default.JsonTaskItemLine,
            cancellationToken))
        {
            if (line is not null)
            {
                yield return new TaskItemLine(
                    line.Title.AsMemory(),
                    line.Description.AsMemory(),
                    line.UserId.AsMemory(),
                    line.DueDate.AsMemory(),
                    line.Status.AsMemory());
            }
        }
    }
}

internal record JsonTaskItemLine(string Title, 
    string Description,
    string UserId,
    string DueDate,
    string Status);

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(JsonTaskItemLine))]
internal partial class TaskItemJsonContext : JsonSerializerContext
{
}
