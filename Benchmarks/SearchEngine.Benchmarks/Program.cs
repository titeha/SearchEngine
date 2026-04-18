using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace SearchEngine.Benchmarks;

public sealed class BenchmarkConfig : ManualConfig
{
  public BenchmarkConfig()
  {
    ArtifactsPath = Path.GetFullPath(Path.Combine([AppContext.BaseDirectory, "..", "..", "BenchmarkDotNet.Artifacts"]));

    var baseJob = Job.Default;

    AddDiagnoser([MemoryDiagnoser.Default]);

    AddJob([baseJob.WithRuntime(CoreRuntime.Core60).WithId(".Net 6")]);
    AddJob([baseJob.WithRuntime(CoreRuntime.Core70).WithId(".Net 7")]);
    AddJob([baseJob.WithRuntime(CoreRuntime.Core80).WithId(".Net 8")]);
    AddJob([baseJob.WithRuntime(CoreRuntime.Core90).WithId(".Net 9")]);
    AddJob([baseJob.WithRuntime(CoreRuntime.Core10_0).WithId(".Net 10")]);
  }
}

internal class Program
{
  static void Main(string[] args)
  {
    BenchmarkSwitcher
      .FromAssembly(typeof(Program).Assembly)
      .Run(args, new BenchmarkConfig());
  }
}
