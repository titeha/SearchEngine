using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты хранилища поискового индекса сервиса.
/// </summary>
public sealed class SearchIndexStoreTests
{
  /// <summary>
  /// Проверяет, что новое хранилище возвращает пустое состояние индекса.
  /// </summary>
  [Fact]
  public void GetStatus_ДоПостроенияИндекса_ВозвращаетПустоеСостояние()
  {
    // Arrange
    SearchIndexStore sut = new();

    // Act
    IndexStatusResponse result = sut.GetStatus();

    // Assert
    Assert.False(result.IsReady);
    Assert.Equal(0, result.DocumentCount);
    Assert.Equal(0, result.SearchableDocumentCount);
    Assert.False(result.IsPhoneticSearch);
    Assert.Null(result.CreatedAtUtc);
  }

  /// <summary>
  /// Проверяет, что поиск до построения индекса возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public void Search_ДоПостроенияИндекса_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexStore sut = new();

    SearchQueryRequest request = new()
    {
      Query = "Иванов"
    };

    // Act
    SearchQueryResponse? result = sut.Search(request, out ApiError? error);

    // Assert
    Assert.Null(result);
    Assert.NotNull(error);
    Assert.Equal("IndexNotBuilt", error.Code);
  }

  /// <summary>
  /// Проверяет, что после построения индекса поиск возвращает найденный документ.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ПослеПостроенияИндекса_ПозволяетВыполнитьПоиск()
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest buildRequest = CreateBuildRequest(
        (1, "Иванов Сергей Петрович"),
        (2, "Папандопуло Александр"));

    // Act
    ApiError? buildError = await sut.BuildAsync(buildRequest);
    IndexStatusResponse status = sut.GetStatus();

    SearchQueryResponse? searchResult = sut.Search(
        new SearchQueryRequest
        {
          Query = "Иванов"
        },
        out ApiError? searchError);

    // Assert
    Assert.Null(buildError);

    Assert.True(status.IsReady);
    Assert.Equal(2, status.DocumentCount);
    Assert.Equal(2, status.SearchableDocumentCount);

    Assert.Null(searchError);
    Assert.NotNull(searchResult);
    Assert.True(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
  }

  /// <summary>
  /// Проверяет, что повторное построение индекса заменяет опубликованный снимок индекса.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ПриПовторномПостроении_ЗаменяетТекущийИндекс()
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest firstBuildRequest = CreateBuildRequest(
        (1, "Иванов Сергей Петрович"));

    IndexBuildRequest secondBuildRequest = CreateBuildRequest(
        (2, "Петров Сергей Петрович"));

    // Act
    ApiError? firstBuildError = await sut.BuildAsync(firstBuildRequest);
    ApiError? secondBuildError = await sut.BuildAsync(secondBuildRequest);

    SearchQueryResponse? oldQueryResult = sut.Search(
        new SearchQueryRequest
        {
          Query = "Иванов"
        },
        out ApiError? oldQueryError);

    SearchQueryResponse? newQueryResult = sut.Search(
        new SearchQueryRequest
        {
          Query = "Петров"
        },
        out ApiError? newQueryError);

    IndexStatusResponse status = sut.GetStatus();

    // Assert
    Assert.Null(firstBuildError);
    Assert.Null(secondBuildError);

    Assert.True(status.IsReady);
    Assert.Equal(1, status.DocumentCount);
    Assert.Equal(1, status.SearchableDocumentCount);

    Assert.Null(oldQueryError);
    Assert.NotNull(oldQueryResult);
    Assert.False(ContainsId(oldQueryResult, 1));

    Assert.Null(newQueryError);
    Assert.NotNull(newQueryResult);
    Assert.True(ContainsId(newQueryResult, 2));
  }

  /// <summary>
  /// Создаёт запрос на построение индекса.
  /// </summary>
  /// <param name="documents">Документы для индексации.</param>
  /// <returns>Запрос на построение индекса.</returns>
  private static IndexBuildRequest CreateBuildRequest(
      params (int Id, string Text)[] documents)
  {
    return new IndexBuildRequest
    {
      IsPhoneticSearch = false,
      Documents = documents
            .Select(document => new IndexDocumentRequest
            {
              Id = document.Id,
              Text = document.Text
            })
            .ToArray()
    };
  }

  /// <summary>
  /// Проверяет наличие идентификатора документа в результате поиска.
  /// </summary>
  /// <param name="response">Ответ поиска.</param>
  /// <param name="id">Идентификатор документа.</param>
  /// <returns><see langword="true"/>, если идентификатор найден.</returns>
  private static bool ContainsId(SearchQueryResponse response, int id)
  {
    return response.Items.Any(bucket => bucket.Ids.Contains(id));
  }
}