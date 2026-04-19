using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace SearchEngine.Benchmarks;

public sealed class BenchmarkConfig : ManualConfig
{
  private const string _runtimeEnvironmentVariable = "SEARCHENGINE_BENCHMARK_RUNTIMES";
  private const string _jobEnvironmentVariable = "SEARCHENGINE_BENCHMARK_JOB";

  private static readonly string[] _defaultRuntimeKeys =
  [
    "net10"
  ];

  private static readonly IReadOnlyDictionary<string, (string Id, CoreRuntime Runtime)> _runtimeMap =
    new Dictionary<string, (string Id, CoreRuntime Runtime)>(StringComparer.OrdinalIgnoreCase)
    {
      ["net6"] = (".NET 6", CoreRuntime.Core60),
      ["net7"] = (".NET 7", CoreRuntime.Core70),
      ["net8"] = (".NET 8", CoreRuntime.Core80),
      ["net9"] = (".NET 9", CoreRuntime.Core90),
      ["net10"] = (".NET 10", CoreRuntime.Core10_0)
    };

  public BenchmarkConfig()
  {
    ArtifactsPath = Path.GetFullPath(
      Path.Combine(
      [
        AppContext.BaseDirectory,
        "..",
        "..",
        "BenchmarkDotNet.Artifacts"
      ]));

    AddDiagnoser([MemoryDiagnoser.Default]);

    Job baseJob = ResolveJob();

    foreach ((string id, CoreRuntime runtime) in ResolveRuntimes())
      AddJob([baseJob.WithRuntime(runtime).WithId(id)]);
  }

  private static Job ResolveJob()
  {
    string? value = Environment.GetEnvironmentVariable(_jobEnvironmentVariable);

    if (string.Equals(value, "short", StringComparison.OrdinalIgnoreCase))
      return Job.ShortRun;

    return Job.Default;
  }

  private static IEnumerable<(string Id, CoreRuntime Runtime)> ResolveRuntimes()
  {
    string? rawValue = Environment.GetEnvironmentVariable(_runtimeEnvironmentVariable);

    string[] runtimeKeys = string.IsNullOrWhiteSpace(rawValue)
      ? _defaultRuntimeKeys
      : rawValue.Split(
          [';', ',', ' '],
          StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (runtimeKeys.Any(key => string.Equals(key, "all", StringComparison.OrdinalIgnoreCase)))
      return _runtimeMap.Values;

    List<(string Id, CoreRuntime Runtime)> runtimes = [];

    foreach (string runtimeKey in runtimeKeys)
    {
      string normalizedKey = NormalizeRuntimeKey(runtimeKey);

      if (!_runtimeMap.TryGetValue(normalizedKey, out (string Id, CoreRuntime Runtime) runtime))
      {
        throw new InvalidOperationException(
          $"Неизвестная целевая платформа бенчмарка: '{runtimeKey}'. " +
          "Допустимые значения: net6, net7, net8, net9, net10, all.");
      }

      runtimes.Add(runtime);
    }

    return runtimes;
  }

  private static string NormalizeRuntimeKey(string value)
  {
    string normalized = value
      .Trim()
      .Replace(".", string.Empty, StringComparison.Ordinal)
      .Replace(" ", string.Empty, StringComparison.Ordinal)
      .ToLowerInvariant();

    return normalized switch
    {
      "6" or "60" or "net6" or "net60" => "net6",
      "7" or "70" or "net7" or "net70" => "net7",
      "8" or "80" or "net8" or "net80" => "net8",
      "9" or "90" or "net9" or "net90" => "net9",
      "10" or "100" or "net10" or "net100" => "net10",
      _ => normalized
    };
  }
}