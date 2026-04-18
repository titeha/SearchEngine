using BenchmarkDotNet.Attributes;

namespace SearchEngine.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class FuzzySearchBenchmarks
{
  [Params(1_000, 10_000, 100_000)]
  public int ItemCount { get; set; }

  [Params(SearchLocation.BeginWord, SearchLocation.InWord)]
  public SearchLocation Location { get; set; }

  [Params(1, 2)]
  public int AcceptableCountMisprint { get; set; }

  [Params("договра", "согласование договра")]
  public string Query { get; set; } = string.Empty;

  private Search<int> _search = null!;
  private SearchRequest _exactRequest = null!;
  private SearchRequest _fuzzyRequest = null!;

  [GlobalSetup]
  public void GlobalSetup()
  {
    _search = new Search<int>();

    _exactRequest = new SearchRequest
    {
      MatchMode = QueryMatchMode.AllTerms,
      SearchType = SearchType.ExactSearch,
      SearchLocation = Location
    };

    _fuzzyRequest = new SearchRequest
    {
      MatchMode = QueryMatchMode.AllTerms,
      SearchType = SearchType.NearSearch,
      SearchLocation = Location,
      AcceptableCountMisprint = AcceptableCountMisprint
    };

    var prepareResult = _search
        .PrepareIndexResult(CreateSource(ItemCount))
        .GetAwaiter()
        .GetResult();

    if (prepareResult.IsFailure)
    {
      throw new InvalidOperationException(
          $"Не удалось подготовить индекс: {prepareResult.Error!.Code} - {prepareResult.Error.Message}");
    }
  }

  [Benchmark(Baseline = true)]
  public int ExactSearch()
  {
    return Execute(_exactRequest);
  }

  [Benchmark]
  public int NearSearch()
  {
    return Execute(_fuzzyRequest);
  }

  private int Execute(SearchRequest request)
  {
    var result = _search.FindResult(Query, request);

    if (result.IsFailure)
    {
      throw new InvalidOperationException(
          $"Поиск завершился с ошибкой: {result.Error!.Code} - {result.Error.Message}");
    }

    return result.Value!.Items.Count;
  }

  private static List<SourceRecord> CreateSource(int itemCount)
  {
    string[] dictionary =
    [
        "согласование",
            "договора",
            "велосипед",
            "красный",
            "горный",
            "процесс",
            "заявка",
            "поставка",
            "доставка",
            "сергей",
            "иванов",
            "петров",
            "документ",
            "маршрут",
            "операция",
            "клиент",
            "заказ",
            "платеж",
            "типовой",
            "контроль"
    ];

    List<SourceRecord> source = new(itemCount);

    for (int i = 0; i < itemCount; i++)
    {
      string text =
          $"{dictionary[i % dictionary.Length]} {dictionary[(i + 3) % dictionary.Length]} {dictionary[(i + 7) % dictionary.Length]}";

      source.Add(new SourceRecord(i + 1, text));
    }

    source[0] = new SourceRecord(1, "согласование договора");
    if (itemCount > 1)
      source[1] = new SourceRecord(2, "согласование типового договора");
    if (itemCount > 2)
      source[2] = new SourceRecord(3, "красный велосипед");
    if (itemCount > 3)
      source[3] = new SourceRecord(4, "горный велосипед");

    return source;
  }

  private sealed record SourceRecord(int Id, string Text) : ISourceData<int>;
}
