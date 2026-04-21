using BenchmarkDotNet.Attributes;

namespace SearchEngine.Benchmarks;

/// <summary>
/// Измеряет построение обычного и фонетического индекса
/// на реальном списке фамилий.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class RealSurnameIndexBuildBenchmarks
{
  private SourceData[] _source = [];

  [Params(1_000, 3_000, 5_000)]
  public int ItemCount { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    string[] surnames = RealSurnameDataLoader.LoadSurnames();

    if (surnames.Length < ItemCount)
    {
      throw new InvalidOperationException(
        $"Для сценария требуется минимум {ItemCount} уникальных фамилий, " +
        $"а найдено только {surnames.Length}. " +
        "Добавьте фамилии в Benchmarks/SearchEngine.Benchmarks/Data/Surnames.");
    }

    _source = new SourceData[ItemCount];

    for (int i = 0; i < _source.Length; i++)
      _source[i] = new SourceData(i, surnames[i]);
  }

  [Benchmark(Baseline = true)]
  public void ExactIndexBuild()
  {
    Search<int> search = new();

    var result = search
      .PrepareIndexResult(_source)
      .GetAwaiter()
      .GetResult();

    if (!result.IsSuccess)
    {
      throw new InvalidOperationException(
        $"Не удалось построить обычный индекс: {result.Error!.Code} - {result.Error.Message}");
    }
  }

  [Benchmark]
  public void PhoneticIndexBuild()
  {
    Search<int> search = new(isPhoneticSearch: true);

    var result = search
      .PrepareIndexResult(_source)
      .GetAwaiter()
      .GetResult();

    if (!result.IsSuccess)
    {
      throw new InvalidOperationException(
        $"Не удалось построить фонетический индекс: {result.Error!.Code} - {result.Error.Message}");
    }
  }

  private sealed record SourceData(int Id, string Text) : ISourceData<int>;
}

/// <summary>
/// Загружает реальные фамилии для benchmark-сценариев.
/// </summary>
internal static class RealSurnameDataLoader
{
  private const string DataDirectoryName = "Data";
  private const string SurnamesDirectoryName = "Surnames";

  /// <summary>
  /// Загружает уникальные фамилии из файлов benchmark-проекта.
  /// </summary>
  /// <returns>Массив уникальных фамилий.</returns>
  public static string[] LoadSurnames()
  {
    string directory = FindSurnamesDirectory();

    string[] files = Directory.GetFiles(
      directory,
      "*.txt",
      SearchOption.TopDirectoryOnly);

    if (files.Length == 0)
    {
      throw new InvalidOperationException(
        $"В каталоге '{directory}' не найдено файлов с фамилиями.");
    }

    SortedSet<string> surnames = new(StringComparer.OrdinalIgnoreCase);

    foreach (string file in files)
    {
      foreach (string line in File.ReadLines(file))
      {
        string surname = NormalizeLine(line);

        if (string.IsNullOrWhiteSpace(surname))
          continue;

        surnames.Add(surname);
      }
    }

    if (surnames.Count == 0)
    {
      throw new InvalidOperationException(
        $"В каталоге '{directory}' не найдено ни одной фамилии.");
    }

    return [.. surnames];
  }

  private static string NormalizeLine(string line)
  {
    string value = line.Trim();

    if (value.Length == 0)
      return string.Empty;

    if (value.StartsWith('#'))
      return string.Empty;

    return value;
  }

  private static string FindSurnamesDirectory()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);

    while (directory is not null)
    {
      string candidate = Path.Combine(
        directory.FullName,
        DataDirectoryName,
        SurnamesDirectoryName);

      if (Directory.Exists(candidate))
        return candidate;

      directory = directory.Parent;
    }

    throw new DirectoryNotFoundException(
      "Не найден каталог с фамилиями. Ожидался путь " +
      "'Benchmarks/SearchEngine.Benchmarks/Data/Surnames'.");
  }
}