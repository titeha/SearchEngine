using Xunit;

namespace SearchEngine.Tests;

public class PhoneticEncoderInjectionTests
{
  [Fact]
  public async Task PrepareIndexResult_ФонетическийПоиск_ДолженИспользоватьВнедренныйКодировщик()
  {
    // Arrange
    static string StubEncoder(string value)
    {
      return value
        .ToUpperInvariant()
        .Replace('О', 'А');
    }

    Search<int> sut = new(
      isPhoneticSearch: true,
      phoneticEncoder: StubEncoder);

    Test<int>[] source =
    [
      new() { Id = 1, Text = "Папондопуло" },
      new() { Id = 2, Text = "Папандопуло" },
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
  public async Task FindResult_ФонетическийПоиск_ДолженИскатьПоНесколькимКлючамКодировщика()
  {
    // Arrange
    static IReadOnlyList<string> StubEncoder(string value)
    {
      return value switch
      {
        "ПЕРВЫЙ" => ["ONE"],
        "ВТОРОЙ" => ["TWO"],
        "ОБА" => ["ONE", "TWO"],
        "ЛИШНИЙ" => ["THREE"],
        _ => []
      };
    }

    Search<int> sut = new(
        isPhoneticSearch: true,
        phoneticKeyEncoder: StubEncoder);

    Test<int>[] source =
    [
        new() { Id = 1, Text = "Первый" },
        new() { Id = 2, Text = "Второй" },
        new() { Id = 3, Text = "Оба" },
        new() { Id = 4, Text = "Лишний" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);
    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult("Оба", request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(ContainsId(result.Value!, 1));
    Assert.True(ContainsId(result.Value!, 2));
    Assert.True(ContainsId(result.Value!, 3));
    Assert.False(ContainsId(result.Value!, 4));
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