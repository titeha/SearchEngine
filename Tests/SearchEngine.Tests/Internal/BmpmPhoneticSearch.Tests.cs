namespace SearchEngine.Tests;

public class BmpmPhoneticSearchTests
{
  [Fact]
  public async Task FindResult_BmpmКодировщик_ДолженИскатьФонетическиБлизкиеФамилии()
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true, phoneticKeyEncoder: BmpmPhoneticEncoder.Encode);

    Test<int>[] source =
    [
        new() { Id = 1, Text = "Иванов" },
            new() { Id = 2, Text = "Иваноф" },
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
    var result = sut.FindResult("Иваноф", request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(ContainsId(result.Value!, 1));
    Assert.True(ContainsId(result.Value!, 2));
    Assert.False(ContainsId(result.Value!, 3));
  }

  [Fact]
  public async Task FindResult_BmpmКодировщик_ДолженУчитыватьНормализациюБуквыЁ()
  {
    // Arrange
    Search<int> sut = new(
        isPhoneticSearch: true,
        phoneticKeyEncoder: BmpmPhoneticEncoder.Encode);

    Test<int>[] source =
    [
        new() { Id = 1, Text = "Семёнов" },
            new() { Id = 2, Text = "Семенов" },
            new() { Id = 3, Text = "Смирнов" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);
    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult("Семенов", request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(ContainsId(result.Value!, 1));
    Assert.True(ContainsId(result.Value!, 2));
    Assert.False(ContainsId(result.Value!, 3));
  }

  [Fact]
  public async Task FindResult_BmpmКодировщик_ДолженИскатьПоГруппамСогласных()
  {
    // Arrange
    Search<int> sut = new(
        isPhoneticSearch: true,
        phoneticKeyEncoder: BmpmPhoneticEncoder.Encode);

    Test<int>[] source =
    [
        new() { Id = 1, Text = "Гончаров" },
            new() { Id = 2, Text = "Кончаров" },
            new() { Id = 3, Text = "Комаров" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);
    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult("Кончаров", request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(ContainsId(result.Value!, 1));
    Assert.True(ContainsId(result.Value!, 2));
    Assert.False(ContainsId(result.Value!, 3));
  }

  private static bool ContainsId(SearchResultList<int> result, int id)
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