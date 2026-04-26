using BenchmarkDotNet.Attributes;

using ResultType;

namespace SearchEngine.Benchmarks;

/// <summary>
/// Измеряет фонетический поиск на наборе с большим количеством уникальных фамилий.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class UniquePhoneticTermsBenchmarks
{
  private Search<int> _exactSearch = null!;
  private Search<int> _phoneticSearch = null!;
  private SearchRequest _request = null!;

  [Params(1_000, 10_000, 100_000)]
  public int ItemCount { get; set; }

  [Params(
    "Папондопуло",
    "Папандопуло",
    "Забабонова",
    "Забабанова",
    "Смирнов",
    "Петров")]
  public string Query { get; set; } = string.Empty;

  /// <summary>
  /// Фонетический алгоритм для проверки.
  /// </summary>
  [Params(
      PhoneticAlgorithmBenchMode.MetaPhone,
      PhoneticAlgorithmBenchMode.Bmpm,
      PhoneticAlgorithmBenchMode.BmpmApprox)]
  public PhoneticAlgorithmBenchMode Algorithm { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    SourceData[] source = CreateSource();

    _exactSearch = new Search<int>();
    _phoneticSearch = PhoneticSearchFactory.Create<int>(Algorithm);

    var exactPrepareResult = _exactSearch
      .PrepareIndexResult(source)
      .GetAwaiter()
      .GetResult();

    if (!exactPrepareResult.IsSuccess)
      throw new InvalidOperationException("Не удалось подготовить обычный индекс для бенчмарка. " + $"{exactPrepareResult.Error!.Code} - {exactPrepareResult.Error.Message}");

    var phoneticPrepareResult = _phoneticSearch
      .PrepareIndexResult(source)
      .GetAwaiter()
      .GetResult();

    if (!phoneticPrepareResult.IsSuccess)
      throw new InvalidOperationException("Не удалось подготовить фонетический индекс для бенчмарка. "
        + $"Алгоритм: {Algorithm}. {phoneticPrepareResult.Error!.Code} - {phoneticPrepareResult.Error.Message}");

    _request = new SearchRequest
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    ValidateSearch(_exactSearch);
    ValidateSearch(_phoneticSearch, Algorithm);
  }

  [Benchmark(Baseline = true)]
  public Result<SearchResultList<int>, SearchError> ExactSearch() => _exactSearch.FindResult(Query, _request);

  [Benchmark]
  public Result<SearchResultList<int>, SearchError> PhoneticSearch() => _phoneticSearch.FindResult(Query, _request);

  private SourceData[] CreateSource()
  {
    SourceData[] source = new SourceData[ItemCount];

    string[] curated =
    [
      "Папондопуло",
      "Папандопуло",
      "Попондопуло",
      "Папондопула",
      "Забабонова",
      "Забабанова",
      "Зобобонова",
      "Смирнов",
      "Смернов",
      "Петров",
      "Петроф"
    ];

    int index = 0;

    for (; index < source.Length && index < curated.Length; index++)
      source[index] = new SourceData(index, curated[index]);

    for (; index < source.Length; index++)
      source[index] = new SourceData(index, CreateSyntheticSurname(index));

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

  /// <summary>
  /// Проверяет, что поисковый движок готов выполнить запрос бенчмарка.
  /// </summary>
  /// <param name="search">Проверяемый поисковый движок.</param>
  /// <param name="algorithm">Фонетический алгоритм, если проверяется фонетический поиск.</param>
  private void ValidateSearch(Search<int> search, PhoneticAlgorithmBenchMode? algorithm = null)
  {
    var result = search.FindResult(Query, _request);

    if (!result.IsSuccess)
      throw new InvalidOperationException("Проверочный поиск завершился ошибкой. " + $"Алгоритм: {algorithm?.ToString() ?? "Exact"}. {result.Error!.Code} - {result.Error.Message}");
  }

  private sealed record SourceData(int Id, string Text) : ISourceData<int>;
}