using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

using ResultType;

namespace SearchEngine.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class ResultCombinationBenchmarks
{
  private Search<int> _search = null!;
  private SearchRequest _request = null!;

  [Params(1_000, 10_000, 100_000)]
  public int ItemCount { get; set; }

  [Params(
    QueryMatchMode.AllTerms,
    QueryMatchMode.AnyTerm,
    QueryMatchMode.SoftAllTerms)]
  public QueryMatchMode MatchMode { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    SourceData[] source = [.. Enumerable
      .Range(0, ItemCount)
      .Select(i => new SourceData(i, "альфа бета гамма"))];

    _search = new Search<int>();

    _search
      .PrepareIndexResult(source)
      .GetAwaiter()
      .GetResult();

    _request = new SearchRequest
    {
      SearchType = SearchType.ExactSearch,
      SearchLocation = SearchLocation.BeginWord,
      MatchMode = MatchMode
    };

    _ = _search.FindResult("альфа", _request);
    _ = _search.FindResult("альфа бета", _request);
    _ = _search.FindResult("альфа бета гамма", _request);
  }

  [Benchmark(Baseline = true)]
  public Result<SearchResultList<int>, SearchError> OneTerm()
  {
    return _search.FindResult("альфа", _request);
  }

  [Benchmark]
  public Result<SearchResultList<int>, SearchError> TwoTerms()
  {
    return _search.FindResult("альфа бета", _request);
  }

  [Benchmark]
  public Result<SearchResultList<int>, SearchError> ThreeTerms()
  {
    return _search.FindResult("альфа бета гамма", _request);
  }

  private sealed record SourceData(int Id, string Text) : ISourceData<int>;
}