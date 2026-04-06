namespace SearchEngine.Tests;

/// <summary>
/// Тесты совместимого безопасного API поиска.
/// </summary>
public class TryFindTests
{
  [Fact]
  public void TryFind_С_Null_ДолженВернутьОшибкуNullSearchString()
  {
    TestSearch<int> sut = new();

    var result = sut.TryFind(null);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.NullSearchString, result.Error.Code);
    Assert.Equal("Поисковая строка не может быть null.", result.Error.Message);
  }

  [Fact]
  public void TryFind_С_ПробельнойСтрокой_ДолженВернутьУспехСПустымРезультатом()
  {
    TestSearch<int> sut = new();

    var result = sut.TryFind(" ");

    Assert.True(result.IsSuccess);
    Assert.False(result.Value!.IsHasIndex);
    Assert.Empty(result.Value.Items);
  }

  [Fact]
  public void TryFind_БезПодготовленногоИндекса_ДолженВернутьОшибкуIndexNotBuilt()
  {
    TestSearch<int> sut = new();

    var result = sut.TryFind("process");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.IndexNotBuilt, result.Error.Code);
    Assert.Equal(
      "Индекс ещё не подготовлен. Выполните PrepareIndex перед поиском.",
      result.Error.Message);
  }

  [Fact]
  public async Task TryFind_ПослеПодготовкиИндекса_ДолженВернутьУспешныйРезультат()
  {
    TestSearch<int> sut = new()
    {
      SearchType = SearchType.ExactSearch
    };

    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.TryFind("process");

    Assert.True(result.IsSuccess);
    Assert.True(result.Value!.IsHasIndex);
    Assert.True(ContainsId(result.Value, 1));
    Assert.True(ContainsId(result.Value, 2));
    Assert.True(ContainsId(result.Value, 3));
  }

  [Fact]
  public async Task TryFind_С_Параметрами_РежимAnyTerm_ДолженВернутьОбъединениеПоСловам()
  {
    TestSearch<int> sut = new();

    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.TryFind(
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
  public async Task TryFind_С_Параметрами_РежимSoftAllTerms_ДолженСтавитьПолноеСовпадениеВышеЧастичных()
  {
    TestSearch<int> sut = new();

    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.TryFind(
      "process ready",
      new SearchRequest
      {
        MatchMode = QueryMatchMode.SoftAllTerms,
        SearchType = SearchType.ExactSearch,
        SearchLocation = SearchLocation.BeginWord,
        PrecisionSearch = 100,
        AcceptableCountMisprint = 0
      });

    Assert.True(result.IsSuccess);
    Assert.True(result.Value!.IsHasIndex);

    Assert.True(ContainsIdAtDistance(result.Value, 0, 2));
    Assert.True(ContainsIdAtDistance(result.Value, 1, 1));
    Assert.True(ContainsIdAtDistance(result.Value, 1, 3));
  }

  [Fact]
  public async Task TryFind_С_ЗапросомБезПригодныхСлов_ДолженВернутьОшибкуQueryHasNoSearchableTerms()
  {
    TestSearch<int> sut = new();

    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.TryFind("a i");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.QueryHasNoSearchableTerms, result.Error.Code);
    Assert.Equal(
      "Поисковый запрос не содержит пригодных для поиска слов.",
      result.Error.Message);
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
}