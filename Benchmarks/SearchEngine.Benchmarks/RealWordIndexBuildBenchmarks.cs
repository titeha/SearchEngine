using BenchmarkDotNet.Attributes;

namespace SearchEngine.Benchmarks;

/// <summary>
/// Измеряет построение обычного поискового индекса
/// на реальном списке слов.
/// </summary>
/// <remarks>
/// В текущей реализации точный и нечёткий поиск используют один и тот же индекс.
/// Поэтому этот benchmark измеряет базовую цену индексации,
/// общую для <see cref="SearchType.ExactSearch"/> и <see cref="SearchType.NearSearch"/>.
/// </remarks>
[Config(typeof(BenchmarkConfig))]
public class RealWordIndexBuildBenchmarks
{
  private SourceData[] _source = [];

  [Params(1_000, 3_000, 5_000, 10_000, 30_000, 50_000)]
  public int ItemCount { get; set; }

  [Params(
    WordIndexSourceMode.UniqueWords,
    WordIndexSourceMode.RepeatedWords,
    WordIndexSourceMode.ShortPhrases)]
  public WordIndexSourceMode SourceMode { get; set; }

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

  [Benchmark(Baseline = true)]
  public void RegularIndexBuild()
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

  private SourceData[] CreateUniqueWordsSource(
    IReadOnlyList<string> words)
  {
    if (words.Count < ItemCount)
    {
      throw new InvalidOperationException(
        $"Для сценария {SourceMode} требуется минимум {ItemCount} слов, " +
        $"а найдено только {words.Count}. Добавьте слова в Data/Words.");
    }

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
/// Описывает тип источника слов для построения индекса.
/// </summary>
public enum WordIndexSourceMode
{
  /// <summary>
  /// Каждая запись содержит уникальное слово.
  /// </summary>
  UniqueWords,

  /// <summary>
  /// Записи используют небольшой повторяющийся набор слов.
  /// </summary>
  RepeatedWords,

  /// <summary>
  /// Каждая запись содержит короткую фразу из нескольких слов.
  /// </summary>
  ShortPhrases
}

/// <summary>
/// Загружает реальные слова для benchmark-сценариев.
/// </summary>
internal static class RealWordDataLoader
{
  private const string DataDirectoryName = "Data";
  private const string WordsDirectoryName = "Words";

  /// <summary>
  /// Загружает уникальные слова из файлов benchmark-проекта.
  /// </summary>
  /// <returns>Массив уникальных слов.</returns>
  public static string[] LoadWords()
  {
    string directory = FindWordsDirectory();

    string[] files = Directory.GetFiles(
      directory,
      "*.txt",
      SearchOption.TopDirectoryOnly);

    if (files.Length == 0)
    {
      throw new InvalidOperationException(
        $"В каталоге '{directory}' не найдено файлов со словами.");
    }

    SortedSet<string> words = new(StringComparer.OrdinalIgnoreCase);

    foreach (string file in files)
    {
      foreach (string line in File.ReadLines(file))
      {
        string word = NormalizeLine(line);

        if (string.IsNullOrWhiteSpace(word))
          continue;

        words.Add(word);
      }
    }

    return [.. words];
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

  private static string FindWordsDirectory()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);

    while (directory is not null)
    {
      string candidate = Path.Combine(
        directory.FullName,
        DataDirectoryName,
        WordsDirectoryName);

      if (Directory.Exists(candidate))
        return candidate;

      directory = directory.Parent;
    }

    throw new DirectoryNotFoundException(
      "Не найден каталог со словами. Ожидался путь " +
      "'Benchmarks/SearchEngine.Benchmarks/Data/Words'.");
  }
}