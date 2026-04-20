namespace SearchEngine.Tests;

public class FindResultTests
{
  [Fact]
  public void FindResult_ДолженВернутьОшибку_ЕслиЗапросПустой()
  {
    TestSearch<int> sut = new();

    var result = sut.FindResult(string.Empty);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.EmptyQuery, result.Error!.Code);
    Assert.Equal("Поисковый запрос пуст.", result.Error.Message);
  }

  [Fact]
  public void FindResult_ДолженВернутьОшибку_ЕслиИндексНеПодготовлен()
  {
    TestSearch<int> sut = new();

    var result = sut.FindResult("process");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.IndexNotBuilt, result.Error!.Code);
    Assert.Equal("Поисковый индекс не подготовлен.", result.Error!.Message);
  }

  [Fact]
  public void FindResult_ДолженВернутьОшибку_ЕслиИндексПуст()
  {
    TestSearch<int> sut = new()
    {
      IsIndexComplete = true
    };

    var result = sut.FindResult("process");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.IndexIsEmpty, result.Error!.Code);
    Assert.Equal("Поисковый индекс пуст.", result.Error.Message);
  }

  [Theory]
  [InlineData(QueryMatchMode.AllTerms)]
  [InlineData(QueryMatchMode.AnyTerm)]
  [InlineData(QueryMatchMode.SoftAllTerms)]
  public async Task FindResult_ForSingleTerm_ReturnsSameResultForAllMatchModes(QueryMatchMode matchMode)
  {
    // arrange
    Search<int> search = new();

    var source = new[]
    {
      new Test<int> { Id = 1, Text = "договор" },
      new Test<int> { Id = 2, Text = "договор поставки" },
      new Test<int> { Id = 3, Text = "акт" }
    };

    await search.PrepareIndexResult(source);

    SearchRequest request = new()
    {
      SearchType = SearchType.ExactSearch,
      SearchLocation = SearchLocation.BeginWord,
      MatchMode = matchMode
    };

    // act
    var result = search.FindResult("договор", request);

    // assert
    Assert.True(result.IsSuccess);
    Assert.Contains(1, result.Value![0].Items);
    Assert.Contains(2, result.Value![0].Items);
    Assert.DoesNotContain(3, result.Value![0].Items);
  }

  [Fact]
  public async Task FindResult_ForSingleFuzzyTerm_KeepsIndexOnlyInBestDistanceBucket()
  {
    // arrange
    Search<int> search = new();

    var source = new[]
    {
      new Test<int> { Id = 1, Text = "договор договра" }
    };

    await search.PrepareIndexResult(source);

    SearchRequest request = new()
    {
      SearchType = SearchType.NearSearch,
      SearchLocation = SearchLocation.BeginWord,
      AcceptableCountMisprint = 1,
      MatchMode = QueryMatchMode.AllTerms
    };

    // act
    var result = search.FindResult("договор", request);

    // assert
    Assert.True(result.IsSuccess);
    Assert.Contains(1, result.Value![0].Items);

    if (result.Value.Items.ContainsKey(1))
      Assert.DoesNotContain(1, result.Value[1].Items);
  }
}