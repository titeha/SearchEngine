namespace SearchEngine.Tests;

public class PhoneticSearchAlgorithmTests
{
  [Theory]
  [InlineData(PhoneticSearchAlgorithm.MetaPhone, "Семёнов", "СИМИНАФ")]
  [InlineData(PhoneticSearchAlgorithm.RussianBmpm, "Семёнов", "СЕМЕНОФ")]
  [InlineData(PhoneticSearchAlgorithm.RussianBmpmApprox, "Семёнов", "СИМИНАФ")]
  public void EncodePhonetic_ВыбранныйАлгоритм_ДолженИспользоватьНужныйКодировщик(
      PhoneticSearchAlgorithm algorithm,
      string source,
      string expectedKey)
  {
    // Arrange
    Search<int> sut = new(
        isPhoneticSearch: true,
        phoneticAlgorithm: algorithm);

    // Act
    string result = sut.EncodePhonetic(source);

    // Assert
    Assert.Equal(expectedKey, result);
  }

  [Fact]
  public async Task FindResult_RussianBmpmApprox_ДолженИскатьСозвучныеФамилии()
  {
    // Arrange
    Search<int> sut = new(
        isPhoneticSearch: true,
        phoneticAlgorithm: PhoneticSearchAlgorithm.RussianBmpmApprox);

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
  public void Constructor_НеизвестныйАлгоритм_ДолженВыброситьИсключение()
  {
    // Act
    var result = Assert.Throws<ArgumentOutOfRangeException>(
        () => new Search<int>(
            isPhoneticSearch: true,
            phoneticAlgorithm: (PhoneticSearchAlgorithm)999));

    // Assert
    Assert.Equal("phoneticAlgorithm", result.ParamName);
  }

  private static bool ContainsId(SearchResultList<int> result, int id)
  {
    foreach (var bucket in result.Items)
      foreach (int item in bucket.Value.Items)
        if (item == id)
          return true;

    return false;
  }
}