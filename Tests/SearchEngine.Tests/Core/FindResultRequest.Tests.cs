namespace SearchEngine.Tests;

/// <summary>
/// Тесты поиска с параметрами запроса.
/// </summary>
public class FindResultRequestTests
{
  [Fact]
  public async Task FindResult_С_Параметрами_Не_Должен_Менять_Разобранные_Слова_Экземпляра()
  {
    TestSearch<int> sut = new();

    var prepareResult = await sut.PrepareIndexResult(Populating.GetTestPopulatedList());
    Assert.True(prepareResult.IsSuccess);

    sut.SearchDisassembling("alpha omega");
    SortedSet<string> expectedSearchList = new() { "ALPHA", "OMEGA" };

    var result = sut.FindResult("process ready", new SearchRequest
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchType = SearchType.NearSearch,
      SearchLocation = SearchLocation.InWord,
      PrecisionSearch = 100,
      AcceptableCountMisprint = 0
    });

    Assert.True(result.IsSuccess);
    Assert.Equal(expectedSearchList, sut.SearchList);
  }

  [Fact]
  public async Task FindResult_С_Параметрами_Не_Должен_Менять_Настройки_Экземпляра()
  {
    TestSearch<int> sut = new()
    {
      SearchType = SearchType.ExactSearch,
      SearchLocation = SearchLocation.BeginWord,
      PrecisionSearch = 75,
      AcceptableCountMisprint = 2
    };

    var prepareResult = await sut.PrepareIndexResult(Populating.GetTestPopulatedList());
    Assert.True(prepareResult.IsSuccess);

    var result = sut.FindResult(
    "process ready",
    new SearchRequest
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchType = SearchType.NearSearch,
      SearchLocation = SearchLocation.InWord,
      PrecisionSearch = 100,
      AcceptableCountMisprint = 0
    });

    Assert.True(result.IsSuccess);
    Assert.Equal(SearchType.ExactSearch, sut.SearchType);
    Assert.Equal(SearchLocation.BeginWord, sut.SearchLocation);
    Assert.Equal(75, sut.PrecisionSearch);
    Assert.Equal(2, sut.AcceptableCountMisprint);
  }
}