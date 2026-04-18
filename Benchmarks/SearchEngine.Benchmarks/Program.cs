using BenchmarkDotNet.Running;

namespace SearchEngine.Benchmarks;

internal class Program
{
  static void Main(string[] args)
  {
    if (args.Length == 0)
    {
      args =
      [
        "--filter",
        "*FuzzySearchBenchmarks*"
      ];
    }

    BenchmarkSwitcher
      .FromAssembly(typeof(Program).Assembly)
      .Run(args);
  }
}
