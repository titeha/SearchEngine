using SearchEngine.Models;

namespace SearchEngine.Tests;

public class RegressionStabilityTests
{
  [Fact]
  public async Task FindResult_С_НедопустимымMatchMode_ДолженВернутьОшибкуInvalidSearchRequest()
  {
    TestSearch<int> sut = new();

    var prepareResult = await sut.PrepareIndexResult(Populating.GetTestPopulatedList());
    Assert.True(prepareResult.IsSuccess);

    var result = sut.FindResult(
      "process",
      new SearchRequest
      {
        MatchMode = (QueryMatchMode)999
      });

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidSearchRequest, result.Error!.Code);
    Assert.Equal(
      "Режим объединения слов запроса имеет недопустимое значение.",
      result.Error.Message);
  }

  [Fact]
  public async Task FindResult_ВРежимеNearSearch_InWord_ДолженНаходитьПодстрокуВДлинномСлове()
  {
    TestSearch<int> sut = new();

    var prepareResult = await sut.PrepareIndexResult(
      [
        new Test<int> { Id = 1, Text = "ultraprocessormodule" }
      ]);
    Assert.True(prepareResult.IsSuccess);

    var result = sut.FindResult(
      "process",
      new SearchRequest
      {
        SearchType = SearchType.NearSearch,
        SearchLocation = SearchLocation.InWord,
        AcceptableCountMisprint = 0
      });

    Assert.True(result.IsSuccess);
    Assert.True(result.Value!.IsHasIndex);
    Assert.True(ContainsId(result.Value, 1));
  }

  [Fact]
  public void FindResult_ВФонетическомРежиме_ДолженСохранятьBucketСДистанциейБольшеНуля()
  {
    TestSearch<int> sut = new(isPhoneticSearch: true, phoneticKeyEncoder: EncodeRegressionPhoneticKey);

    sut.SearchDisassembling("abc");
    string query = Assert.Single(sut.SearchList);
    string target = CreatePhoneticTargetAtDistanceOne(query);

    int expectedDistance = Search<int>.Levenshtein.DistanceLevenshtein(
      query,
      target[..(query.Length + 1)]);

    Assert.Equal(1, expectedDistance);

    sut.SearchIndex.Add(target, new IndexList<int>(5));
    sut.IsIndexComplete = true;

    var result = sut.FindResult("abc");

    Assert.True(result.IsSuccess);
    Assert.True(result.Value!.IsHasIndex);
    Assert.True(ContainsIdAtDistance(result.Value, expectedDistance, 5));
  }

  /// <summary>
  /// Возвращает стабильный фонетический ключ для regression-тестов.
  /// </summary>
  /// <param name="source">Исходная строка.</param>
  /// <returns>Набор фонетических ключей.</returns>
  private static IReadOnlyList<string> EncodeRegressionPhoneticKey(string source)
  {
    if (string.IsNullOrWhiteSpace(source))
      return [];

    return [source.ToUpperInvariant()];
  }

  private static string CreatePhoneticTargetAtDistanceOne(string query)
  {
    const string replacements = "ABCDEFGHIJKLMNOPQRSTUVWXYZАБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ0123456789";
    const string suffixes = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    for (int position = 0; position < query.Length; position++)
    {
      foreach (char replacement in replacements)
      {
        if (replacement == query[position])
          continue;

        string prefix = $"{query[..position]}{replacement}{query[(position + 1)..]}";

        if (!HasDistance(query, prefix, 0))
          continue;

        foreach (char suffix in suffixes)
        {
          string candidate = $"{prefix}{suffix}";

          if (candidate.StartsWith(query, StringComparison.Ordinal))
            continue;

          if (HasDistance(query, candidate[..(query.Length + 1)], 1))
            return candidate;
        }
      }
    }

    throw new Xunit.Sdk.XunitException(
      $"Не удалось подобрать target с дистанцией 1 для '{query}'.");
  }

  private static bool HasDistance(string left, string right, int expected)
  {
    try
    {
      return Search<int>.Levenshtein.DistanceLevenshtein(left, right) == expected;
    }
    catch (KeyNotFoundException)
    {
      return false;
    }
  }

  private static bool ContainsId(SearchResultList<int> result, int id)
  {
    foreach (var bucket in result.Items)
      foreach (int item in bucket.Value.Items)
        if (item == id)
          return true;

    return false;
  }

  private static bool ContainsIdAtDistance(SearchResultList<int> result, int distance, int id)
  {
    if (!result.Items.TryGetValue(distance, out var bucket))
      return false;

    foreach (int item in bucket.Items)
      if (item == id)
        return true;

    return false;
  }
}