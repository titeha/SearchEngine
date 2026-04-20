using BenchmarkDotNet.Attributes;

using ResultType;

namespace SearchEngine.Benchmarks;

/// <summary>
/// Измеряет стоимость объединения результатов поиска при разной степени пересечения
/// списков идентификаторов между словами запроса.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ResultOverlapBenchmarks
{
  private const string Query = "альфа бета гамма";

  private Search<int> _search = null!;
  private SearchRequest _request = null!;

  [Params(10_000, 100_000)]
  public int ItemCount { get; set; }

  [Params(
    ResultOverlapMode.FullOverlap,
    ResultOverlapMode.PartialOverlap,
    ResultOverlapMode.NoOverlap)]
  public ResultOverlapMode OverlapMode { get; set; }

  [Params(
    QueryMatchMode.AllTerms,
    QueryMatchMode.AnyTerm,
    QueryMatchMode.SoftAllTerms)]
  public QueryMatchMode MatchMode { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    SourceData[] source = CreateSource();

    _search = new Search<int>();

    var prepareResult = _search
      .PrepareIndexResult(source)
      .GetAwaiter()
      .GetResult();

    if (!prepareResult.IsSuccess)
      throw new InvalidOperationException("Не удалось подготовить индекс для бенчмарка.");

    _request = new SearchRequest
    {
      SearchType = SearchType.ExactSearch,
      SearchLocation = SearchLocation.BeginWord,
      MatchMode = MatchMode
    };

    ValidateSearchResult();
  }

  [Benchmark]
  public Result<SearchResultList<int>, SearchError> Search()
  {
    return _search.FindResult(Query, _request);
  }

  private SourceData[] CreateSource()
  {
    SourceData[] source = new SourceData[ItemCount];

    for (int i = 0; i < source.Length; i++)
    {
      source[i] = new SourceData(
        i,
        CreateText(i));
    }

    return source;
  }

  private string CreateText(int index)
  {
    return OverlapMode switch
    {
      ResultOverlapMode.FullOverlap => CreateFullOverlapText(),

      ResultOverlapMode.PartialOverlap => CreatePartialOverlapText(index),

      ResultOverlapMode.NoOverlap => CreateNoOverlapText(index),

      _ => throw new InvalidOperationException(
        $"Неизвестный режим пересечения результатов: {OverlapMode}.")
    };
  }

  private static string CreateFullOverlapText()
  {
    return "альфа бета гамма";
  }

  private static string CreatePartialOverlapText(int index)
  {
    return (index % 6) switch
    {
      0 => "альфа",
      1 => "бета",
      2 => "гамма",
      3 => "альфа бета",
      4 => "бета гамма",
      5 => "альфа бета гамма",
      _ => throw new InvalidOperationException("Недостижимая ветка распределения данных.")
    };
  }

  private static string CreateNoOverlapText(int index)
  {
    return (index % 3) switch
    {
      0 => "альфа",
      1 => "бета",
      2 => "гамма",
      _ => throw new InvalidOperationException("Недостижимая ветка распределения данных.")
    };
  }

  private void ValidateSearchResult()
  {
    var result = _search.FindResult(Query, _request);

    if (!result.IsSuccess)
      throw new InvalidOperationException("Проверочный поиск завершился ошибкой.");

    int actualCount = CountAllIndexes(result.Value!);
    int expectedCount = GetExpectedResultCount();

    if (actualCount != expectedCount)
    {
      throw new InvalidOperationException(
        $"Неожиданное количество результатов для сценария {OverlapMode} / {MatchMode}. " +
        $"Ожидалось: {expectedCount}, получено: {actualCount}.");
    }
  }

  private int GetExpectedResultCount()
  {
    if (MatchMode is QueryMatchMode.AnyTerm or QueryMatchMode.SoftAllTerms)
      return ItemCount;

    return OverlapMode switch
    {
      ResultOverlapMode.FullOverlap => ItemCount,
      ResultOverlapMode.PartialOverlap => CountItemsByModulo(ItemCount, 6, 5),
      ResultOverlapMode.NoOverlap => 0,

      _ => throw new InvalidOperationException(
        $"Неизвестный режим пересечения результатов: {OverlapMode}.")
    };
  }

  private static int CountItemsByModulo(
    int itemCount,
    int modulo,
    int expectedRemainder)
  {
    int count = 0;

    for (int i = expectedRemainder; i < itemCount; i += modulo)
      count++;

    return count;
  }

  private static int CountAllIndexes(SearchResultList<int> result)
  {
    int count = 0;

    foreach (var bucket in result.Items)
      count += bucket.Value.Count;

    return count;
  }

  private sealed record SourceData(int Id, string Text) : ISourceData<int>;
}

/// <summary>
/// Описывает степень пересечения результатов между словами поискового запроса.
/// </summary>
public enum ResultOverlapMode
{
  /// <summary>
  /// Все записи содержат все слова запроса.
  /// </summary>
  FullOverlap,

  /// <summary>
  /// Часть записей содержит одно слово, часть — два слова, часть — все слова запроса.
  /// </summary>
  PartialOverlap,

  /// <summary>
  /// Каждая запись содержит только одно слово запроса, пересечения между всеми словами нет.
  /// </summary>
  NoOverlap
}