namespace SearchEngine.Tests;

public class DefaultPhoneticSearchTests
{
  [Theory]
  [InlineData("Иванов", "ИФАНАФ")]
  [InlineData("Папондопуло", "ПАПАНТАПУЛА")]
  [InlineData("Забабонова", "САПАПАНАФА")]
  public void EncodePhonetic_ФонетическийПоискПоУмолчанию_ДолженИспользоватьBmpmApprox(
      string source,
      string expected)
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    // Act
    string result = sut.EncodePhonetic(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Fact]
  public async Task FindResult_ФонетическийПоискПоУмолчанию_ДолженИскатьСозвучныеФамилии()
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    Test<int>[] source =
    [
        new() { Id = 1, Text = "Папандопуло" },
            new() { Id = 2, Text = "Папондопуло" },
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
    var result = sut.FindResult("Папондопуло", request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(ContainsId(result.Value!, 1));
    Assert.True(ContainsId(result.Value!, 2));
    Assert.False(ContainsId(result.Value!, 3));
  }

  [Fact]
  public void EncodePhonetic_Латиница_ДолженИспользоватьРезервныйКодировщик()
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    // Act
    string result = sut.EncodePhonetic("Ivanov");

    // Assert
    Assert.False(string.IsNullOrWhiteSpace(result));
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