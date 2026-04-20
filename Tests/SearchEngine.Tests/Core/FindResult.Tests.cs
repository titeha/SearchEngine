using SearchEngine.Models;

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

  [Fact]
  public async Task FindResult_ExactSearchAllTerms_ДолженВозвращатьТолькоЗаписиСоВсемиСловами()
  {
    // Arrange
    Search<int> sut = new();

    Test<int>[] source =
    [
      new() { Id = 1, Text = "альфа бета гамма" },
    new() { Id = 2, Text = "альфа бета" },
    new() { Id = 3, Text = "бета гамма" },
    new() { Id = 4, Text = "альфа" },
    new() { Id = 5, Text = "дельта" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      SearchType = SearchType.ExactSearch,
      SearchLocation = SearchLocation.BeginWord,
      MatchMode = QueryMatchMode.AllTerms
    };

    // Act
    var result = sut.FindResult("альфа бета гамма", request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(ContainsIdAtDistance(result.Value!, 0, 1));
    Assert.Equal(1, CountAllIndexes(result.Value!));
  }

  [Fact]
  public async Task FindResult_ExactSearchAnyTerm_ДолженВозвращатьОбъединениеБезДублей()
  {
    // Arrange
    Search<int> sut = new();

    Test<int>[] source =
    [
      new() { Id = 1, Text = "альфа бета гамма" },
    new() { Id = 2, Text = "альфа бета" },
    new() { Id = 3, Text = "бета гамма" },
    new() { Id = 4, Text = "альфа" },
    new() { Id = 5, Text = "дельта" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      SearchType = SearchType.ExactSearch,
      SearchLocation = SearchLocation.BeginWord,
      MatchMode = QueryMatchMode.AnyTerm
    };

    // Act
    var result = sut.FindResult("альфа бета гамма", request);

    // Assert
    Assert.True(result.IsSuccess);

    Assert.True(ContainsIdAtDistance(result.Value!, 0, 1));
    Assert.True(ContainsIdAtDistance(result.Value!, 0, 2));
    Assert.True(ContainsIdAtDistance(result.Value!, 0, 3));
    Assert.True(ContainsIdAtDistance(result.Value!, 0, 4));
    Assert.False(ContainsId(result.Value!, 5));

    Assert.Equal(4, CountAllIndexes(result.Value!));
  }

  [Fact]
  public async Task FindResult_ExactSearchSoftAllTerms_ДолженГруппироватьПоКоличествуПропущенныхСлов()
  {
    // Arrange
    Search<int> sut = new();

    Test<int>[] source =
    [
      new() { Id = 1, Text = "альфа бета гамма" },
    new() { Id = 2, Text = "альфа бета" },
    new() { Id = 3, Text = "бета гамма" },
    new() { Id = 4, Text = "альфа" },
    new() { Id = 5, Text = "дельта" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      SearchType = SearchType.ExactSearch,
      SearchLocation = SearchLocation.BeginWord,
      MatchMode = QueryMatchMode.SoftAllTerms
    };

    // Act
    var result = sut.FindResult("альфа бета гамма", request);

    // Assert
    Assert.True(result.IsSuccess);

    Assert.True(ContainsIdAtDistance(result.Value!, 0, 1));

    Assert.True(ContainsIdAtDistance(result.Value!, 1, 2));
    Assert.True(ContainsIdAtDistance(result.Value!, 1, 3));

    Assert.True(ContainsIdAtDistance(result.Value!, 2, 4));

    Assert.False(ContainsId(result.Value!, 5));

    Assert.Equal(4, CountAllIndexes(result.Value!));
  }

  private static bool ContainsId(
    SearchResultList<int> result,
    int id)
  {
    foreach (var bucket in result.Items)
    {
      foreach (int item in bucket.Value.Items)
      {
        if (item == id)
          return true;
      }
    }

    return false;
  }

  private static bool ContainsIdAtDistance(
    SearchResultList<int> result,
    int distance,
    int id)
  {
    if (!result.Items.TryGetValue(distance, out IndexList<int>? bucket))
      return false;

    foreach (int item in bucket.Items)
    {
      if (item == id)
        return true;
    }

    return false;
  }

  private static int CountAllIndexes(SearchResultList<int> result)
  {
    int count = 0;

    foreach (var bucket in result.Items)
      count += bucket.Value.Count;

    return count;
  }
}