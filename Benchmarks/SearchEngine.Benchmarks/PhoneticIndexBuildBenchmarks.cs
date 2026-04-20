using BenchmarkDotNet.Attributes;

namespace SearchEngine.Benchmarks;

/// <summary>
/// Измеряет стоимость построения обычного и фонетического индекса.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class PhoneticIndexBuildBenchmarks
{
  private static readonly string[] _repeatedNames =
  [
    "Егор",
    "Игорь",
    "Терентьев",
    "Тирентев",
    "Соловьёв",
    "Салавьев",
    "Фёдор",
    "Федор",
    "Смирнов",
    "Смернов",
    "Петров",
    "Петроф",
    "Гоголадзе",
    "Коколадзе",
    "Иванов",
    "Иванова",
    "Алексей",
    "Алексеи",
    "Никита",
    "Никитин"
  ];

  private SourceData[] _source = [];

  [Params(1_000, 10_000, 100_000)]
  public int ItemCount { get; set; }

  [Params(
    PhoneticIndexSourceMode.RepeatedNames,
    PhoneticIndexSourceMode.UniqueSurnames)]
  public PhoneticIndexSourceMode SourceMode { get; set; }

  [GlobalSetup]
  public void Setup() => _source = CreateSource();

  [Benchmark(Baseline = true)]
  public void ExactIndexBuild()
  {
    Search<int> search = new();

    var result = search
      .PrepareIndexResult(_source)
      .GetAwaiter()
      .GetResult();

    if (!result.IsSuccess)
      throw new InvalidOperationException($"Не удалось построить обычный индекс: {result.Error!.Code} - {result.Error.Message}");
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
      throw new InvalidOperationException($"Не удалось построить фонетический индекс: {result.Error!.Code} - {result.Error.Message}");
  }

  private SourceData[] CreateSource()
  {
    SourceData[] source = new SourceData[ItemCount];

    for (int i = 0; i < source.Length; i++)
    {
      string text = SourceMode switch
      {
        PhoneticIndexSourceMode.RepeatedNames => _repeatedNames[i % _repeatedNames.Length],
        PhoneticIndexSourceMode.UniqueSurnames => CreateSyntheticSurname(i),
        _ => throw new InvalidOperationException(
          $"Неизвестный режим источника данных: {SourceMode}.")
      };

      source[i] = new SourceData(i, text);
    }

    return source;
  }

  private static string CreateSyntheticSurname(int value)
  {
    const string consonants = "бвгджзклмнпрстфхцчш";

    Span<char> chars = stackalloc char[10];

    chars[0] = 'Б';
    chars[1] = consonants[value % consonants.Length];
    chars[2] = 'а';
    chars[3] = consonants[(value / consonants.Length) % consonants.Length];
    chars[4] = 'о';
    chars[5] = consonants[(value / (consonants.Length * consonants.Length)) % consonants.Length];
    chars[6] = 'е';
    chars[7] = consonants[(value / (consonants.Length * consonants.Length * consonants.Length)) % consonants.Length];
    chars[8] = 'о';
    chars[9] = 'в';

    return new string(chars);
  }

  private sealed record SourceData(int Id, string Text) : ISourceData<int>;
}

/// <summary>
/// Описывает тип набора данных для построения фонетического индекса.
/// </summary>
public enum PhoneticIndexSourceMode
{
  /// <summary>
  /// Набор содержит много повторяющихся имён и фамилий.
  /// </summary>
  RepeatedNames,

  /// <summary>
  /// Набор содержит много уникальных синтетических фамилий.
  /// </summary>
  UniqueSurnames
}