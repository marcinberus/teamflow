using System.Runtime.CompilerServices;
using System.Text.Json;
using TeamFlow.Importing.Common;
using TeamFlow.Importing.FileExtensions;
using TeamFlow.Importing.Projects.Models;

namespace TeamFlow.Importing.Projects.Importerts;

public class JsonDeserializeImporter : IProjectImporter
{
    public bool CanImport(FileExtension fileExtension) => fileExtension == FileExtension.Json;

    private readonly static JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
            JsonSerializer.DeserializeAsyncEnumerable<ProjectLine>(stream, Options, cancellationToken))
        {
            if (line is not null)
            {
                yield return line;
            }
        }
    }
}
