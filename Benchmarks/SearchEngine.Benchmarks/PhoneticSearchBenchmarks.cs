using BenchmarkDotNet.Attributes;

using ResultType;

namespace SearchEngine.Benchmarks;

/// <summary>
/// Измеряет скорость текущего фонетического поиска на наборе русских имён и фамилий.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class PhoneticSearchBenchmarks
{
  private static readonly string[] _names =
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

  private Search<int> _exactSearch = null!;
  private Search<int> _phoneticSearch = null!;
  private SearchRequest _request = null!;

  [Params(1_000, 10_000, 100_000)]
  public int ItemCount { get; set; }

  [Params(
    "Егор",
    "Игорь",
    "Терентьев",
    "Тирентев",
    "Соловьев",
    "Салавьев",
    "Федор",
    "Смирнов",
    "Петров",
    "Гоголадзе")]
  public string Query { get; set; } = string.Empty;

  /// <summary>
  /// Фонетический алгоритм для проверки.
  /// </summary>
  [Params(
      PhoneticAlgorithmBenchMode.MetaPhone,
      PhoneticAlgorithmBenchMode.Bmpm)]
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
      throw new InvalidOperationException("Не удалось подготовить обычный индекс для бенчмарка.");

    var phoneticPrepareResult = _phoneticSearch
      .PrepareIndexResult(source)
      .GetAwaiter()
      .GetResult();

    if (!phoneticPrepareResult.IsSuccess)
      throw new InvalidOperationException($"Не удалось подготовить фонетический индекс для бенчмарка. Алгоритм: {Algorithm}.");

    _request = new SearchRequest
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    ValidateSearch(_exactSearch);
    ValidateSearch(_phoneticSearch, Algorithm);
  }

  [Benchmark(Baseline = true)]
  public Result<SearchResultList<int>, SearchError> ExactSearch()
  {
    return _exactSearch.FindResult(Query, _request);
  }

  [Benchmark]
  public Result<SearchResultList<int>, SearchError> PhoneticSearch()
  {
    return _phoneticSearch.FindResult(Query, _request);
  }

  private SourceData[] CreateSource()
  {
    SourceData[] source = new SourceData[ItemCount];

    for (int i = 0; i < source.Length; i++)
    {
      source[i] = new SourceData(
        i,
        _names[i % _names.Length]);
    }

    return source;
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
      throw new InvalidOperationException($"Проверочный поиск завершился ошибкой. Алгоритм: {algorithm?.ToString() ?? "Exact"}. {result.Error!.Code} - {result.Error.Message}");
  }

  private sealed record SourceData(int Id, string Text) : ISourceData<int>;
}