using BenchmarkDotNet.Attributes;
using System.Text;
using TeamFlow.Importing;
using TeamFlow.Importing.Projects.Importerts;
using MemoryCsvImporter = TeamFlow.Importing.TaskItems.Importers.CsvImporter;
using SpanCsvImporter = TeamFlow.Importing.Projects.Importerts.CsvImporter;

namespace TeamFlow.Benchmarks;

[MemoryDiagnoser]
public class CsvImporterBenchmarks
{
    private static readonly byte[] CsvContent = Encoding.UTF8.GetBytes(CreateCsvContent());

    private readonly CsvSplitImporter _splitImporter = new();
    private readonly SpanCsvImporter _spanImporter = new();
    private readonly MemoryCsvImporter _memoryImporter = new();

    [Benchmark(Baseline = true, Description = "string.Split")]
    public Task<int> ImportWithSplitAsync() => ImportAllAsync(_splitImporter);

    [Benchmark(Description = "ReadOnlySpan<char>")]
    public Task<int> ImportWithSpanAsync() => ImportAllAsync(_spanImporter);

    [Benchmark(Description = "ReadOnlyMemory<char>")]
    public Task<int> ImportWithMemoryAsync() => ImportAllAsync(_memoryImporter);

    private static async Task<int> ImportAllAsync<T>(IImporter<T> importer) where T : IImportLine
    {
        using var stream = new MemoryStream(CsvContent, writable: false);
        var importedCount = 0;

        await foreach (var _ in importer.Import(stream, CancellationToken.None))
        {
            importedCount++;
        }

        return importedCount;
    }

    private static string CreateCsvContent()
    {
        return string.Join(
            Environment.NewLine,
            Enumerable.Range(1, 1_000)
                .Select(index => $"\"Project {index}\",\"Description for project {index}\""));
    }
}