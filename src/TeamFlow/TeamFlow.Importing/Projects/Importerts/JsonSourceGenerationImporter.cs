using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeamFlow.Importing.Common;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Importing.Projects.Importerts;

public class JsonSourceGenerationImporter : IProjectImporter
{
    public bool CanImport(FileExtension fileExtension) => fileExtension == FileExtension.Json;

    public async IAsyncEnumerable<ProjectLine> Import(
    Stream stream,
    [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
        {
            throw new ArgumentException(ErrorMessages.StreamUnreadable);
        }

        await foreach (var line in
            JsonSerializer.DeserializeAsyncEnumerable(stream,
            MetadataJsonContext.Default.ProjectLine,
            cancellationToken))
        {
            if (line == null)
            {
                continue;
            }

            yield return line;
        }
    }
}

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ProjectLine))]
internal partial class MetadataJsonContext : JsonSerializerContext
{
}