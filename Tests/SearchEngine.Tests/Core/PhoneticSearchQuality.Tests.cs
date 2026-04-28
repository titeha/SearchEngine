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

  [Fact]
  public async Task FindResult_ФонетическийПоиск_НеДолженСчитатьРазныеКлючиОдинаковойДлиныСовпадением()
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    Test<int>[] source =
    [
      new() { Id = 1, Text = "Иванов" },
    new() { Id = 2, Text = "Петров" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult("Петров", request);

    // Assert
    Assert.True(result.IsSuccess);

    Assert.True(ContainsId(result.Value!, 2));
    Assert.False(ContainsId(result.Value!, 1));
  }

  [Theory]
  [InlineData("Папондопуло", "Папандопуло")]
  [InlineData("Папондопуло", "Попондопуло")]
  [InlineData("Забабонова", "Забабанова")]
  [InlineData("Забабонова", "Зобобонова")]
  public async Task FindResult_ФонетическийПоиск_ДолженНаходитьВариантыТрудныхФамилий(
  string query,
  string variant)
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    Test<int>[] source =
    [
      new() { Id = 1, Text = variant },
    new() { Id = 2, Text = "Петров" },
    new() { Id = 3, Text = "Иванов" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult(query, request);

    // Assert
    Assert.True(result.IsSuccess);

    Assert.True(ContainsId(result.Value!, 1));
    Assert.False(ContainsId(result.Value!, 2));
    Assert.False(ContainsId(result.Value!, 3));
  }

  [Fact]
  public async Task FindResult_ФонетическийПоиск_ДолженНаходитьФамилиюПоНачалу()
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    Test<int>[] source =
    [
      new() { Id = 1, Text = "Папандопуло" },
    new() { Id = 2, Text = "Петров" },
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
    // Запрос "Папандо" уже достаточно длинный для фонетического
    // префиксного поиска. Более короткий вариант "Папанд"
    // проверяется отдельным защитным тестом.
    var result = sut.FindResult("Папандо", request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(ContainsId(result.Value!, 1));
    Assert.False(ContainsId(result.Value!, 2));
    Assert.False(ContainsId(result.Value!, 3));
  }

  [Fact]
  public async Task FindResult_ФонетическийПоиск_НеДолженСчитатьЛюбойКороткийПрефиксСовпадением()
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    Test<int>[] source =
    [
      new() { Id = 1, Text = "Папандопуло" }
    ];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(prepareResult.IsSuccess);

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult("Папанд", request);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.False(ContainsId(result.Value!, 1));
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