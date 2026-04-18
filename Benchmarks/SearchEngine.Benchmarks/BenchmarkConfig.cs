using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace SearchEngine.Benchmarks;

public sealed class BenchmarkConfig : ManualConfig
{
  public BenchmarkConfig()
  {
    ArtifactsPath = Path.GetFullPath(Path.Combine([AppContext.BaseDirectory, "..", "..", "BenchmarkDotNet.Artifacts"]));

    var baseJob = Job.Default;

    //AddLogger([ConsoleLogger.Default]);
    //AddExporter([MarkdownExporter.GitHub]);
    //AddExporter([HtmlExporter.Default]);
    //AddExporter([CsvExporter.Default]);
    //AddColumnProvider(DefaultColumnProviders.Instance);

    AddDiagnoser([MemoryDiagnoser.Default]);

    AddJob([baseJob.WithRuntime(CoreRuntime.Core60).WithId(".Net 6")]);
    AddJob([baseJob.WithRuntime(CoreRuntime.Core70).WithId(".Net 7")]);
    AddJob([baseJob.WithRuntime(CoreRuntime.Core80).WithId(".Net 8")]);
    AddJob([baseJob.WithRuntime(CoreRuntime.Core90).WithId(".Net 9")]);
    AddJob([baseJob.WithRuntime(CoreRuntime.Core10_0).WithId(".Net 10")]);
  }
}
