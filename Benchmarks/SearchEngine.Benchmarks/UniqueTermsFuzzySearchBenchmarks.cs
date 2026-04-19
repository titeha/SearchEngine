using BenchmarkDotNet.Attributes;

using ResultType;

namespace SearchEngine.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class UniqueTermsFuzzySearchBenchmarks
{
  private Search<int> _search = null!;
  private SearchRequest _exactRequest = null!;
  private SearchRequest _fuzzyRequest = null!;

  private string _exactQuery = string.Empty;
  private string _fuzzyQuery = string.Empty;

  [Params(1_000, 10_000, 100_000)]
  public int UniqueTermCount { get; set; }

  [Params(SearchLocation.BeginWord, SearchLocation.InWord)]
  public SearchLocation Location { get; set; }

  [Params(1, 2)]
  public int AcceptableCountMisprint { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    SourceData[] source = [.. Enumerable
      .Range(0, UniqueTermCount)
      .Select(i => new SourceData(i, CreateUniqueWord(i)))];

    _search = new Search<int>();

    _search
      .PrepareIndexResult(source)
      .GetAwaiter()
      .GetResult();

    _exactRequest = new SearchRequest
    {
      SearchType = SearchType.ExactSearch,
      SearchLocation = Location,
      MatchMode = QueryMatchMode.AllTerms
    };

    _fuzzyRequest = new SearchRequest
    {
      SearchType = SearchType.NearSearch,
      SearchLocation = Location,
      MatchMode = QueryMatchMode.AllTerms,
      AcceptableCountMisprint = AcceptableCountMisprint
    };

    _exactQuery = CreateUniqueWord(UniqueTermCount / 2);
    _fuzzyQuery = MakeTypo(_exactQuery);

    _ = _search.FindResult(_exactQuery, _exactRequest);
    _ = _search.FindResult(_fuzzyQuery, _fuzzyRequest);
  }

  [Benchmark(Baseline = true)]
  public Result<SearchResultList<int>, SearchError> ExactSearch()
  {
    return _search.FindResult(_exactQuery, _exactRequest);
  }

  [Benchmark]
  public Result<SearchResultList<int>, SearchError> FuzzySearch()
  {
    return _search.FindResult(_fuzzyQuery, _fuzzyRequest);
  }

  private static string CreateUniqueWord(int value)
  {
    const string alphabet = "абвгдежзиклмнопрстуфхцчшэюя";

    char[] result = new char[8];

    int number = value + 1;

    for (int i = 0; i < result.Length; i++)
    {
      result[i] = alphabet[number % alphabet.Length];
      number = number / alphabet.Length + i * 17 + value;
    }

    return new string(result);
  }

  private static string MakeTypo(string value)
  {
    char[] result = value.ToCharArray();

    int position = result.Length / 2;
    result[position] = result[position] == 'о' ? 'а' : 'о';

    return new string(result);
  }

  private sealed record SourceData(int Id, string Text) : ISourceData<int>;
}