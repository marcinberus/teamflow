using System.Text;
using BenchmarkDotNet.Attributes;
using TeamFlow.Importing.Projects;
using TeamFlow.Importing.Projects.Importerts;

namespace TeamFlow.Benchmarks;

[MemoryDiagnoser]
public class CsvImporterBenchmarks
{
    private static readonly byte[] CsvContent = Encoding.UTF8.GetBytes(CreateCsvContent());

    private readonly CsvImporter _spanImporter = new();
    private readonly CsvSplitImporter _splitImporter = new();

    [Benchmark(Baseline = true, Description = "string.Split")]
    public Task<int> ImportWithSplitAsync() => ImportAllAsync(_splitImporter);

    [Benchmark(Description = "ReadOnlySpan<char>")]
    public Task<int> ImportWithSpanAsync() => ImportAllAsync(_spanImporter);

    private static async Task<int> ImportAllAsync(IProjectImporter importer)
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