using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты хранилища поисковых индексов в режиме нескольких именованных индексов.
/// </summary>
public sealed class SearchIndexStoreMultiIndexTests
{
  /// <summary>
  /// Проверяет, что именованные индексы изолированы: поиск идёт только по своему индексу.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ДваИменованныхИндекса_ИзолированыПриПоиске()
  {
    // Arrange
    SearchIndexStore sut = new();

    await sut.BuildAsync(CreateBuildRequest("products", (1, "Красный велосипед")));
    await sut.BuildAsync(CreateBuildRequest("people", (2, "Иванов Сергей Петрович")));

    // Act
    SearchQueryResponse? productsResult = sut.Search(
        new SearchQueryRequest { Index = "products", Query = "велосипед" },
        out ApiError? productsError);

    SearchQueryResponse? peopleResult = sut.Search(
        new SearchQueryRequest { Index = "people", Query = "велосипед" },
        out ApiError? peopleError);

    // Assert
    Assert.Null(productsError);
    Assert.NotNull(productsResult);
    Assert.True(ContainsId(productsResult, 1));

    Assert.Null(peopleError);
    Assert.NotNull(peopleResult);
    Assert.False(ContainsId(peopleResult, 1));
  }

  /// <summary>
  /// Проверяет, что построение именованного индекса не затрагивает индекс по умолчанию.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ИменованныйИндекс_НеЗатрагиваетИндексПоУмолчанию()
  {
    // Arrange
    SearchIndexStore sut = new();

    // Act
    await sut.BuildAsync(CreateBuildRequest("products", (1, "Красный велосипед")));

    IndexStatusResponse defaultStatus = sut.GetStatus();
    IndexStatusResponse productsStatus = sut.GetStatus("products");

    // Assert
    Assert.Equal(IndexState.NotBuilt, defaultStatus.State);
    Assert.False(defaultStatus.IsReady);

    Assert.Equal(IndexState.Ready, productsStatus.State);
    Assert.Equal("products", productsStatus.IndexName);
    Assert.True(productsStatus.IsReady);
  }

  /// <summary>
  /// Проверяет, что два индекса можно строить параллельно.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ПараллельноеПостроениеДвухИндексов_СтроитОба()
  {
    // Arrange
    SearchIndexStore sut = new();

    // Act
    await Task.WhenAll(
        sut.BuildAsync(CreateBuildRequest("products", (1, "Красный велосипед"))),
        sut.BuildAsync(CreateBuildRequest("people", (2, "Иванов Сергей Петрович"))));

    // Assert
    Assert.Equal(IndexState.Ready, sut.GetStatus("products").State);
    Assert.Equal(IndexState.Ready, sut.GetStatus("people").State);
    Assert.Equal(2, sut.GetAllStatuses().Count);
  }

  /// <summary>
  /// Проверяет, что недопустимое имя индекса возвращает прикладную ошибку.
  /// </summary>
  /// <param name="indexName">Недопустимое имя индекса.</param>
  [Theory]
  [InlineData("../escape")]
  [InlineData("bad/name")]
  [InlineData("имя")]
  public async Task BuildAsync_СНедопустимымИменемИндекса_ВозвращаетОшибку(string indexName)
  {
    // Arrange
    SearchIndexStore sut = new();

    // Act
    ApiError? error = await sut.BuildAsync(CreateBuildRequest(indexName, (1, "Красный велосипед")));

    // Assert
    Assert.NotNull(error);
    Assert.Equal("InvalidIndexName", error.Code);
  }

  /// <summary>
  /// Проверяет, что поиск по неизвестному индексу возвращает ошибку IndexNotBuilt.
  /// </summary>
  [Fact]
  public void Search_ПоНеизвестномуИндексу_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexStore sut = new();

    // Act
    SearchQueryResponse? result = sut.Search(
        new SearchQueryRequest { Index = "unknown", Query = "Иванов" },
        out ApiError? error);

    // Assert
    Assert.Null(result);
    Assert.NotNull(error);
    Assert.Equal("IndexNotBuilt", error.Code);
  }

  /// <summary>
  /// Создаёт запрос на построение именованного индекса.
  /// </summary>
  /// <param name="index">Имя индекса.</param>
  /// <param name="documents">Документы для индексации.</param>
  /// <returns>Запрос на построение индекса.</returns>
  private static IndexBuildRequest CreateBuildRequest(string index, params (int Id, string Text)[] documents)
  {
    return new IndexBuildRequest
    {
      Index = index,
      Documents = [.. documents.Select(document => new IndexDocumentRequest
      {
        Id = document.Id,
        Text = document.Text
      })]
    };
  }

  /// <summary>
  /// Проверяет наличие идентификатора документа в результате поиска.
  /// </summary>
  /// <param name="response">Ответ поиска.</param>
  /// <param name="id">Идентификатор документа.</param>
  /// <returns><see langword="true"/>, если идентификатор найден.</returns>
  private static bool ContainsId(SearchQueryResponse response, int id) => response.Items.Any(bucket => bucket.Ids.Contains(id));
}
