namespace SearchEngine.Tests;

/// <summary>
/// Тесты валидации строковых источников для безопасной подготовки индекса.
/// </summary>
public class PrepareIndexResultValidationTests
{
  [Fact]
  public async Task PrepareIndexResult_ДолженВернутьОшибку_ЕслиВСтрокеНеНайденРазделитель()
  {
    TestSearch<int> sut = new();

    var result = await sut.PrepareIndexResult(
      [
        "1 Check the process"
      ],
      ";");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidDelimitedSourceFormat, result.Error!.Code);
    Assert.Equal(
      "Строка 1: В строке не найден разделитель между идентификатором и текстом.",
      result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task PrepareIndexResult_ДолженВернутьОшибку_ЕслиИдентификаторНеПолучилосьПреобразовать()
  {
    TestSearch<int> sut = new();

    var result = await sut.PrepareIndexResult(
      [
        "abc;Check the process"
      ],
      ";");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidIdFormat, result.Error!.Code);
    Assert.Equal(
      "Строка 1: Не удалось преобразовать идентификатор \"abc\" к типу Int32.",
      result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task PrepareIndexResult_ДолженВернутьОшибку_ЕслиТекстЗаписиПуст()
  {
    TestSearch<int> sut = new();

    var result = await sut.PrepareIndexResult(
      [
        "1;"
      ],
      ";");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidDelimitedSourceFormat, result.Error!.Code);
    Assert.Equal(
      "Строка 1: Не указан текст записи.",
      result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task PrepareIndexResult_ДолженВернутьОшибку_ЕслиСтрокаИсточникаПуста()
  {
    TestSearch<int> sut = new();

    var result = await sut.PrepareIndexResult(
      [
        ""
      ],
      ";");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidSourceRecord, result.Error!.Code);
    Assert.Equal(
      "Строка 1: Строка источника пуста.",
      result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task PrepareIndexResult_ДолженСохранятьВесьТекстПослеПервогоРазделителя()
  {
    TestSearch<int> sut = new();

    var prepareResult = await sut.PrepareIndexResult(
      new[]
      {
        "1;товар;с дополнительным;разделителем"
      },
      ";");

    Assert.True(prepareResult.IsSuccess);
    Assert.True(sut.IsIndexComplete);

    var searchResult = sut.FindResult("разделителем");

    Assert.True(searchResult.IsSuccess);
    Assert.True(searchResult.Value!.IsHasIndex);
    Assert.True(ContainsId(searchResult.Value, 1));
  }

  /// <summary>
  /// Проверяет, содержится ли идентификатор в результатах поиска.
  /// </summary>
  /// <param name="result">Результат поиска.</param>
  /// <param name="id">Искомый идентификатор.</param>
  /// <returns>
  /// <see langword="true"/>, если идентификатор найден; иначе <see langword="false"/>.
  /// </returns>
  private static bool ContainsId(SearchResultList<int> result, int id)
  {
    foreach (var bucket in result.Items)
    {
      string[] parts = bucket.Value
        .ToString()
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

      if (parts.Any(x => x == id.ToString()))
      {
        return true;
      }
    }

    return false;
  }
}