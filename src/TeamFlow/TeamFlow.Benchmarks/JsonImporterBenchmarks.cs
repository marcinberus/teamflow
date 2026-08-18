using BenchmarkDotNet.Attributes;
using System.Text;
using TeamFlow.Importing;
using TeamFlow.Importing.Projects.Importerts;
using MemoryCsvImporter = TeamFlow.Importing.TaskItems.Importers.CsvImporter;
using SpanCsvImporter = TeamFlow.Importing.Projects.Importerts.CsvImporter;

namespace TeamFlow.Benchmarks;

[MemoryDiagnoser]
public class JsonImporterBenchmarks
{
    private static readonly byte[] JsonContent = Encoding.UTF8.GetBytes(CreateJsonContent());

    private readonly JsonDeserializeImporter _jsonDeserializeImporter = new();
    private readonly JsonSourceGenerationImporter _jsonDeserializeSourceGenMetadataImporter = new();

    [Benchmark(Baseline = true, Description = "Deserialize")]
    public Task<int> ImportWithDeserializeAsync() => ImportAllAsync(_jsonDeserializeImporter);

    [Benchmark(Description = "Source generation Metadata")]
    public Task<int> ImportSourceGenMetadataAsync() => ImportAllAsync(_jsonDeserializeSourceGenMetadataImporter);

    private static async Task<int> ImportAllAsync<T>(IImporter<T> importer) where T : IImportLine
    {
        using var stream = new MemoryStream(JsonContent, writable: false);
        var importedCount = 0;

        await foreach (var _ in importer.Import(stream, CancellationToken.None))
        {
            importedCount++;
        }

        return importedCount;
    }

    private static string CreateJsonContent()
    {
        return new StringBuilder()
            .Append('[')
            .Append(string.Join(',', Enumerable.Range(1, 1_000)
                        .Select(index => $"{{\"name\":\"Project {index}\",\"Description\":\"Description for project {index}\"}}")))
            .Append(']')
            .ToString();
    }
}