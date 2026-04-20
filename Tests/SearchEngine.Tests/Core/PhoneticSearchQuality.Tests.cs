namespace SearchEngine.Tests;

public class PhoneticSearchQualityTests
{
  [Fact]
  public async Task FindResult_ФонетическийПоиск_ДолженНаходитьФонетическиБлизкиеРусскиеИмена()
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    Test<int>[] source =
    [
      new() { Id = 1, Text = "Егор" },
      new() { Id = 2, Text = "Игорь" },
      new() { Id = 3, Text = "Терентьев" },
      new() { Id = 4, Text = "Тирентев" },
      new() { Id = 5, Text = "Петров" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult("Егор", request);

    // Assert
    Assert.True(result.IsSuccess);

    Assert.True(ContainsId(result.Value!, 1));
    Assert.True(ContainsId(result.Value!, 2));

    Assert.False(ContainsId(result.Value!, 5));
  }

  [Fact]
  public async Task FindResult_ФонетическийПоиск_ДолженНаходитьВариантыФамилии()
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    Test<int>[] source =
    [
      new() { Id = 1, Text = "Терентьев" },
      new() { Id = 2, Text = "Тирентев" },
      new() { Id = 3, Text = "Егор" },
      new() { Id = 4, Text = "Петров" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult("Терентьев", request);

    // Assert
    Assert.True(result.IsSuccess);

    Assert.True(ContainsId(result.Value!, 1));
    Assert.True(ContainsId(result.Value!, 2));

    Assert.False(ContainsId(result.Value!, 4));
  }

  [Fact]
  public async Task FindResult_ФонетическийПоиск_ДолженВозвращатьУспешныйПустойРезультатЕслиСовпаденийНет()
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    Test<int>[] source =
    [
      new() { Id = 1, Text = "Егор" },
      new() { Id = 2, Text = "Терентьев" },
      new() { Id = 3, Text = "Петров" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult("Александр", request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.False(result.Value!.IsHasIndex);
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
}