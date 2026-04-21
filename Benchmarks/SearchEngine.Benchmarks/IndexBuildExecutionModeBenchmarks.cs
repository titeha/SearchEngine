using BenchmarkDotNet.Attributes;

namespace SearchEngine.Benchmarks;

/// <summary>
/// Сравнивает автоматический, последовательный и принудительно параллельный режимы
/// построения поискового индекса.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class IndexBuildExecutionModeBenchmarks
{
  private const int ForceSequentialThreshold = int.MaxValue;

  private SourceData[] _source = [];

  [Params(1_000, 10_000, 50_000)]
  public int ItemCount { get; set; }

  [Params(
    WordIndexSourceMode.UniqueWords,
    WordIndexSourceMode.RepeatedWords,
    WordIndexSourceMode.ShortPhrases)]
  public WordIndexSourceMode SourceMode { get; set; }

  [Params(
    IndexBuildExecutionMode.Automatic,
    IndexBuildExecutionMode.ForceSequential,
    IndexBuildExecutionMode.ForceParallel)]
  public IndexBuildExecutionMode ExecutionMode { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    string[] words = RealWordDataLoader.LoadWords();

    if (words.Length == 0)
      throw new InvalidOperationException("Список слов пуст.");

    _source = SourceMode switch
    {
      WordIndexSourceMode.UniqueWords => CreateUniqueWordsSource(words),
      WordIndexSourceMode.RepeatedWords => CreateRepeatedWordsSource(words),
      WordIndexSourceMode.ShortPhrases => CreateShortPhrasesSource(words),

      _ => throw new InvalidOperationException(
        $"Неизвестный режим источника слов: {SourceMode}.")
    };
  }

  [Benchmark]
  public void BuildIndex()
  {
    Search<int> search = new();

    bool forceParallel = ExecutionMode == IndexBuildExecutionMode.ForceParallel;

    int parallelProcessingThreshold = ExecutionMode == IndexBuildExecutionMode.ForceSequential
      ? ForceSequentialThreshold
      : 10_000;

    var result = search
      .PrepareIndexResult(_source, forceParallel: forceParallel, parallelProcessingThreshold: parallelProcessingThreshold)
      .GetAwaiter()
      .GetResult();

    if (!result.IsSuccess)
      throw new InvalidOperationException($"Не удалось построить индекс: {result.Error!.Code} - {result.Error.Message}");
  }

  private SourceData[] CreateUniqueWordsSource(
    IReadOnlyList<string> words)
  {
    if (words.Count < ItemCount)
      throw new InvalidOperationException($"Для сценария {SourceMode} требуется минимум {ItemCount} слов, " + $"а найдено только {words.Count}. Добавьте слова в Data/Words.");

    SourceData[] source = new SourceData[ItemCount];

    for (int i = 0; i < source.Length; i++)
      source[i] = new SourceData(i, words[i]);

    return source;
  }

  private SourceData[] CreateRepeatedWordsSource(
    IReadOnlyList<string> words)
  {
    SourceData[] source = new SourceData[ItemCount];

    int poolSize = Math.Min(words.Count, 100);

    if (poolSize == 0)
      throw new InvalidOperationException("Недостаточно слов для сценария с повторами.");

    for (int i = 0; i < source.Length; i++)
      source[i] = new SourceData(i, words[i % poolSize]);

    return source;
  }

  private SourceData[] CreateShortPhrasesSource(
    IReadOnlyList<string> words)
  {
    if (words.Count < 3)
      throw new InvalidOperationException("Для сценария коротких фраз нужно минимум три слова.");

    SourceData[] source = new SourceData[ItemCount];

    for (int i = 0; i < source.Length; i++)
    {
      string first = words[i % words.Count];
      string second = words[(i + 17) % words.Count];
      string third = words[(i + 43) % words.Count];

      source[i] = new SourceData(
        i,
        $"{first} {second} {third}");
    }

    return source;
  }

  private sealed record SourceData(int Id, string Text) : ISourceData<int>;
}

/// <summary>
/// Описывает режим выполнения построения индекса.
/// </summary>
public enum IndexBuildExecutionMode
{
  /// <summary>
  /// Используется стандартная политика библиотеки.
  /// </summary>
  Automatic,

  /// <summary>
  /// Параллельная обработка отключена через высокий порог.
  /// </summary>
  ForceSequential,

  /// <summary>
  /// Параллельная обработка включена принудительно.
  /// </summary>
  ForceParallel
}