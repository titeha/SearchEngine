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
  public async Task FindResult_С_Параметрами_РежимAnyTerm_ДолженВозвращатьОбъединениеПоСловам()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "process date",
      new SearchRequest
      {
        MatchMode = QueryMatchMode.AnyTerm,
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
    Assert.True(ContainsId(result.Value, 5));
    Assert.False(ContainsId(result.Value, 4));
  }

  [Fact]
  public async Task FindResult_С_Параметрами_РежимAnyTerm_ДолженИспользоватьЛучшуюДистанциюБезДубликатов()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "process proces",
      new SearchRequest
      {
        MatchMode = QueryMatchMode.AnyTerm,
        SearchType = SearchType.NearSearch,
        AcceptableCountMisprint = 1
      });

    Assert.True(result.IsSuccess);
    Assert.True(result.Value!.IsHasIndex);

    Assert.True(ContainsId(result.Value, 1));
    Assert.True(ContainsId(result.Value, 2));
    Assert.True(ContainsId(result.Value, 3));

    Assert.Equal(1, CountOccurrences(result.Value, 1));
    Assert.Equal(1, CountOccurrences(result.Value, 2));
    Assert.Equal(1, CountOccurrences(result.Value, 3));
  }

  [Fact]
  public async Task FindResult_С_Параметрами_РежимAllTerms_ДолженВозвращатьТолькоОбщиеИдентификаторы_ЕслиОдноСловоНайденоНеточно()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "procwss ready",
      new SearchRequest
      {
        MatchMode = QueryMatchMode.AllTerms,
        SearchType = SearchType.NearSearch,
        SearchLocation = SearchLocation.BeginWord,
        AcceptableCountMisprint = 1
      });

    Assert.True(result.IsSuccess);
    Assert.True(result.Value!.IsHasIndex);

    Assert.True(ContainsId(result.Value, 2));
    Assert.False(ContainsId(result.Value, 1));
    Assert.False(ContainsId(result.Value, 3));
    Assert.True(ContainsIdAtDistance(result.Value, 1, 2));
  }

  [Fact]
  public async Task FindResult_С_Параметрами_РежимAllTerms_ДолженСуммироватьЛучшуюДистанциюПоВсемСловам()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult(
      "procwss readu",
      new SearchRequest
      {
        MatchMode = QueryMatchMode.AllTerms,
        SearchType = SearchType.NearSearch,
        SearchLocation = SearchLocation.BeginWord,
        AcceptableCountMisprint = 1
      });

    Assert.True(result.IsSuccess);
    Assert.True(result.Value!.IsHasIndex);
    Assert.True(ContainsIdAtDistance(result.Value, 2, 2));
    Assert.Single(result.Value.Items[2].Items);
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
      "process ready",
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
    Assert.True(ContainsId(result.Value, 2));
    Assert.False(ContainsId(result.Value, 1));
    Assert.False(ContainsId(result.Value, 3));
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
      foreach (int item in bucket.Value.Items)
        if (item == id)
          return true;

    return false;
  }

  /// <summary>
  /// Проверяет, находится ли идентификатор в корзине с указанной дистанцией.
  /// </summary>
  /// <param name="result">Результат поиска.</param>
  /// <param name="distance">Дистанция поиска.</param>
  /// <param name="id">Искомый идентификатор.</param>
  /// <returns>
  /// <see langword="true"/>, если идентификатор найден в нужной корзине;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool ContainsIdAtDistance(SearchResultList<int> result, int distance, int id)
  {
    if (!result.Items.TryGetValue(distance, out IndexList<int>? bucket))
      return false;

    foreach (int item in bucket.Items)
      if (item == id)
        return true;

    return false;
  }

  /// <summary>
  /// Подсчитывает количество вхождений идентификатора в результатах поиска.
  /// </summary>
  /// <param name="result">Результат поиска.</param>
  /// <param name="id">Идентификатор записи.</param>
  /// <returns>Количество найденных вхождений.</returns>
  private static int CountOccurrences(SearchResultList<int> result, int id)
  {
    int count = 0;

    foreach (var bucket in result.Items)
      foreach (int item in bucket.Value.Items)
        if (item == id)
          count++;

    return count;
  }
}