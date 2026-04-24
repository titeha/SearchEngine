namespace SearchEngine.Tests;

public class PhoneticSearchQualityMatrixTests
{
  public static TheoryData<PhoneticSearchCase> Cases =>
  [
    new PhoneticSearchCase(
      Name: "Русские имена",
      Query: "Егор",
      Source:
      [
        (1, "Егор"),
        (2, "Игорь"),
        (3, "Петров")
      ],
      ExpectedIds: [1, 2],
      ForbiddenIds: [3]),

    new PhoneticSearchCase(
      Name: "Варианты фамилии Терентьев",
      Query: "Терентьев",
      Source:
      [
        (1, "Терентьев"),
        (2, "Тирентев"),
        (3, "Петров")
      ],
      ExpectedIds: [1, 2],
      ForbiddenIds: [3]),

    new PhoneticSearchCase(
      Name: "Папондопуло и близкий вариант",
      Query: "Папондопуло",
      Source:
      [
        (1, "Папондопуло"),
        (2, "Папандопуло"),
        (3, "Петров")
      ],
      ExpectedIds: [1, 2],
      ForbiddenIds: [3]),

    new PhoneticSearchCase(
      Name: "Забабонова и близкий вариант",
      Query: "Забабонова",
      Source:
      [
        (1, "Забабонова"),
        (2, "Забабанова"),
        (3, "Петров")
      ],
      ExpectedIds: [1, 2],
      ForbiddenIds: [3]),

    new PhoneticSearchCase(
      Name: "Поиск по фонетическому префиксу",
      Query: "Папандо",
      Source:
      [
        (1, "Папандопуло"),
        (2, "Петров"),
        (3, "Смирнов")
      ],
      ExpectedIds: [1],
      ForbiddenIds: [2, 3])
  ];

  [Theory]
  [MemberData(nameof(Cases))]
  public async Task FindResult_ФонетическийПоиск_ДолженДаватьОжидаемыеСовпадения(
    PhoneticSearchCase testCase)
  {
    // Arrange
    Search<int> sut = new(isPhoneticSearch: true);

    Test<int>[] source = [.. testCase
      .Source
      .Select(x => new Test<int> { Id = x.Id, Text = x.Text })];

    var prepareResult = await sut.PrepareIndexResult(source);

    Assert.True(
      prepareResult.IsSuccess,
      $"{testCase.Name}: индекс не подготовлен.");

    SearchRequest request = new()
    {
      MatchMode = QueryMatchMode.AnyTerm,
      SearchLocation = SearchLocation.BeginWord
    };

    // Act
    var result = sut.FindResult(testCase.Query, request);

    // Assert
    Assert.True(
      result.IsSuccess,
      $"{testCase.Name}: поиск завершился ошибкой.");

    foreach (int expectedId in testCase.ExpectedIds)
    {
      Assert.True(
        ContainsId(result.Value!, expectedId),
        $"{testCase.Name}: ожидалось совпадение с Id={expectedId}.");
    }

    foreach (int forbiddenId in testCase.ForbiddenIds)
    {
      Assert.False(
        ContainsId(result.Value!, forbiddenId),
        $"{testCase.Name}: не ожидалось совпадение с Id={forbiddenId}.");
    }
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

public sealed record PhoneticSearchCase(
  string Name,
  string Query,
  (int Id, string Text)[] Source,
  int[] ExpectedIds,
  int[] ForbiddenIds);