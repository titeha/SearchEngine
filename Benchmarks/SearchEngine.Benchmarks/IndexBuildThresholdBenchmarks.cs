using BenchmarkDotNet.Attributes;

namespace SearchEngine.Benchmarks;

/// <summary>
/// Проверяет поведение построения индекса около порога автоматической параллельной обработки.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class IndexBuildThresholdBenchmarks
{
  private SourceData[] _source = [];

  [Params(75_000, 100_000, 150_000, 200_000, 250_000, 300_000)]
  public int ItemCount { get; set; }

  [Params(
    IndexThresholdSourceMode.UniqueWords,
    IndexThresholdSourceMode.RepeatedWords,
    IndexThresholdSourceMode.ShortPhrases)]
  public IndexThresholdSourceMode SourceMode { get; set; }

  [Params(
    IndexBuildExecutionMode.Automatic,
    IndexBuildExecutionMode.ForceSequential,
    IndexBuildExecutionMode.ForceParallel)]
  public IndexBuildExecutionMode ExecutionMode { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    _source = SourceMode switch
    {
      IndexThresholdSourceMode.UniqueWords => CreateUniqueWordsSource(),
      IndexThresholdSourceMode.RepeatedWords => CreateRepeatedWordsSource(),
      IndexThresholdSourceMode.ShortPhrases => CreateShortPhrasesSource(),

      _ => throw new InvalidOperationException(
        $"Неизвестный режим источника данных: {SourceMode}.")
    };
  }

  [Benchmark]
  public void BuildIndex()
  {
    Search<int> search = new();

    var result = ExecutionMode switch
    {
      IndexBuildExecutionMode.Automatic => search
        .PrepareIndexResult(_source)
        .GetAwaiter()
        .GetResult(),

      IndexBuildExecutionMode.ForceSequential => search
        .PrepareIndexResult(
          _source,
          forceParallel: false,
          parallelProcessingThreshold: int.MaxValue)
        .GetAwaiter()
        .GetResult(),

      IndexBuildExecutionMode.ForceParallel => search
        .PrepareIndexResult(
          _source,
          forceParallel: true,
          parallelProcessingThreshold: 0)
        .GetAwaiter()
        .GetResult(),

      _ => throw new InvalidOperationException(
        $"Неизвестный режим построения индекса: {ExecutionMode}.")
    };

    if (!result.IsSuccess)
      throw new InvalidOperationException($"Не удалось построить индекс: {result.Error!.Code} - {result.Error.Message}");
  }

  private SourceData[] CreateUniqueWordsSource()
  {
    SourceData[] source = new SourceData[ItemCount];

    for (int i = 0; i < source.Length; i++)
      source[i] = new SourceData(i, CreateUniqueWord(i));

    return source;
  }

  private SourceData[] CreateRepeatedWordsSource()
  {
    SourceData[] source = new SourceData[ItemCount];

    const int poolSize = 100;

    for (int i = 0; i < source.Length; i++)
      source[i] = new SourceData(i, CreateUniqueWord(i % poolSize));

    return source;
  }

  private SourceData[] CreateShortPhrasesSource()
  {
    SourceData[] source = new SourceData[ItemCount];

    for (int i = 0; i < source.Length; i++)
    {
      string first = CreateUniqueWord(i);
      string second = CreateUniqueWord(i + 17);
      string third = CreateUniqueWord(i + 43);

      source[i] = new SourceData(
        i,
        $"{first} {second} {third}");
    }

    return source;
  }

  private static string CreateUniqueWord(int value)
  {
    const string alphabet = "абвгдежзиклмнопрстуфхцчшэюя";

    Span<char> chars = stackalloc char[10];

    int number = value + 1;

    for (int i = 0; i < chars.Length; i++)
    {
      chars[i] = alphabet[number % alphabet.Length];
      number = number / alphabet.Length + i * 17 + value;
    }

    return new string(chars);
  }

  private sealed record SourceData(int Id, string Text) : ISourceData<int>;
}

/// <summary>
/// Описывает тип источника для проверки порога параллельной индексации.
/// </summary>
public enum IndexThresholdSourceMode
{
  /// <summary>
  /// Все записи содержат уникальные слова.
  /// </summary>
  UniqueWords,

  /// <summary>
  /// Записи используют небольшой повторяющийся набор слов.
  /// </summary>
  RepeatedWords,

  /// <summary>
  /// Каждая запись содержит короткую фразу из трёх слов.
  /// </summary>
  ShortPhrases
}