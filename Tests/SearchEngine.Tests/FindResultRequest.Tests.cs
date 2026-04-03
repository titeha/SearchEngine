namespace SearchEngine.Tests;

/// <summary>
/// Тесты поиска с параметрами запроса.
/// </summary>
public class FindResultRequestTests
{
  [Fact]
  public async Task FindResult_С_Параметрами_ДолженВернутьОшибку_ЕслиТочностьМеньшеНуля()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "process",
      new SearchRequest
      {
        PrecisionSearch = -1
      });

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidSearchRequest, result.Error!.Code);
    Assert.Equal("Точность поиска должна быть в диапазоне от 0 до 100.", result.Error.Message);
  }

  [Fact]
  public async Task FindResult_С_Параметрами_ДолженВернутьОшибку_ЕслиТочностьБольшеСта()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "process",
      new SearchRequest
      {
        PrecisionSearch = 101
      });

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidSearchRequest, result.Error!.Code);
    Assert.Equal("Точность поиска должна быть в диапазоне от 0 до 100.", result.Error.Message);
  }

  [Fact]
  public async Task FindResult_С_Параметрами_ДолженВернутьОшибку_ЕслиКоличествоОпечатокОтрицательное()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "process",
      new SearchRequest
      {
        AcceptableCountMisprint = -1
      });

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidSearchRequest, result.Error!.Code);
    Assert.Equal("Допустимое количество опечаток не может быть отрицательным.", result.Error.Message);
  }

  [Fact]
  public async Task FindResult_С_Параметрами_ДолженВернутьОшибку_ЕслиРежимAnyTermПокаНеПоддерживается()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "process ready",
      new SearchRequest
      {
        MatchMode = QueryMatchMode.AnyTerm
      });

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidSearchRequest, result.Error!.Code);
    Assert.Equal("Режим AnyTerm пока не поддерживается.", result.Error.Message);
  }

  [Fact]
  public async Task FindResult_С_Параметрами_ДолженВернутьОшибку_ЕслиРежимSoftAllTermsПокаНеПоддерживается()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "process ready",
      new SearchRequest
      {
        MatchMode = QueryMatchMode.SoftAllTerms
      });

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidSearchRequest, result.Error!.Code);
    Assert.Equal("Режим SoftAllTerms пока не поддерживается.", result.Error.Message);
  }

  [Fact]
  public async Task FindResult_С_Параметрами_ДолженВернутьРезультат_ДляПоддерживаемогоЗапроса()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "process",
      new SearchRequest
      {
        MatchMode = QueryMatchMode.AllTerms,
        SearchType = SearchType.ExactSearch,
        SearchLocation = SearchLocation.BeginWord,
        PrecisionSearch = 100,
        AcceptableCountMisprint = 0
      });

    Assert.True(result.IsSuccess);
    Assert.True(result.Value!.IsHasIndex);
    Assert.True(ContainsId(result.Value, 1));
    Assert.True(ContainsId(result.Value, 2));
    Assert.True(ContainsId(result.Value, 3));
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