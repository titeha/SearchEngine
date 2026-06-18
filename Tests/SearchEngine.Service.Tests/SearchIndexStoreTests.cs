using Microsoft.Extensions.Options;

using SearchEngine;
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
    Assert.Equal(IndexState.NotBuilt, result.State);
    Assert.False(result.IsReady);
    Assert.Equal(0, result.DocumentCount);
    Assert.Equal(0, result.SearchableDocumentCount);
    Assert.False(result.IsPhoneticSearch);
    Assert.Null(result.CreatedAtUtc);
  }

  /// <summary>
  /// Проверяет, что после построения индекса состояние становится Ready.
  /// </summary>
  [Fact]
  public async Task GetStatus_ПослеПостроенияИндекса_ВозвращаетСостояниеReady()
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest buildRequest = CreateBuildRequest(
        [(1, "Иванов Сергей Петрович")]);

    // Act
    await sut.BuildAsync(buildRequest);
    IndexStatusResponse status = sut.GetStatus();

    // Assert
    Assert.Equal(IndexState.Ready, status.State);
    Assert.True(status.IsReady);
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
        [(1, "Иванов Сергей Петрович"),
        (2, "Папандопуло Александр")]);

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
        [(1, "Иванов Сергей Петрович")]);

    IndexBuildRequest secondBuildRequest = CreateBuildRequest(
        [(2, "Петров Сергей Петрович")]);

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
  /// Проверяет, что без фонетического индекса латинская запись не находит кириллическую фамилию.
  /// </summary>
  [Fact]
  public async Task Search_БезФонетическогоИндекса_НеИщетРусскуюФамилиюВЛатинскойЗаписи()
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest buildRequest = CreateBuildRequest(
        isPhoneticSearch: false,
        [(1, "Иванов Сергей Петрович"),
        (2, "Папандопуло Александр")]);

    ApiError? buildError = await sut.BuildAsync(buildRequest);

    // Act
    SearchQueryResponse? searchResult = sut.Search(
        new SearchQueryRequest
        {
          Query = "Ivanov"
        },
        out ApiError? searchError);

    // Assert
    Assert.Null(buildError);
    Assert.Null(searchError);
    Assert.NotNull(searchResult);
    Assert.False(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
  }

  /// <summary>
  /// Проверяет, что фонетический индекс ищет русские фамилии в латинской записи.
  /// </summary>
  /// <param name="query">Поисковая строка в латинской записи.</param>
  /// <param name="expectedId">Ожидаемый идентификатор документа.</param>
  [Theory]
  [InlineData("Ivanov", 1)]
  [InlineData("Papandopulo", 2)]
  [InlineData("Papondopulo", 2)]
  public async Task Search_СФонетическимИндексом_ИщетРусскиеФамилииВЛатинскойЗаписи(
      string query,
      int expectedId)
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest buildRequest = CreateBuildRequest(
        isPhoneticSearch: true,
        [(1, "Иванов Сергей Петрович"),
        (2, "Папандопуло Александр"),
        (3, "Красный велосипед")]);

    ApiError? buildError = await sut.BuildAsync(buildRequest);

    // Act
    SearchQueryResponse? searchResult = sut.Search(
        new SearchQueryRequest
        {
          Query = query
        },
        out ApiError? searchError);

    // Assert
    Assert.Null(buildError);
    Assert.Null(searchError);
    Assert.NotNull(searchResult);
    Assert.True(ContainsId(searchResult, expectedId));
  }

  /// <summary>
  /// Проверяет, что нечёткий поиск через допустимое количество опечаток находит документ.
  /// </summary>
  /// <param name="query">Поисковая строка с опечаткой.</param>
  /// <param name="acceptableCountMisprint">Допустимая взвешенная дистанция.</param>
  [Theory]
  [InlineData("Иваноы", 1)]
  [InlineData("Иваноф", 2)]
  public async Task Search_НечеткийПоискЧерезКоличествоОпечаток_НаходитДокумент(
      string query,
      int acceptableCountMisprint)
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest buildRequest = CreateBuildRequest(
        [(1, "Иванов Сергей Петрович"),
        (2, "Папандопуло Александр"),
        (3, "Красный велосипед")]);

    ApiError? buildError = await sut.BuildAsync(buildRequest);

    // Act
    SearchQueryResponse? searchResult = sut.Search(
        new SearchQueryRequest
        {
          Query = query,
          SearchType = SearchType.NearSearch,
          SearchLocation = SearchLocation.BeginWord,
          AcceptableCountMisprint = acceptableCountMisprint
        },
        out ApiError? searchError);

    // Assert
    Assert.Null(buildError);
    Assert.Null(searchError);
    Assert.NotNull(searchResult);
    Assert.True(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
    Assert.False(ContainsId(searchResult, 3));
  }

  /// <summary>
  /// Проверяет, что нечёткий поиск через процент точности находит документ.
  /// </summary>
  [Fact]
  public async Task Search_НечеткийПоискЧерезПроцентТочности_НаходитДокумент()
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest buildRequest = CreateBuildRequest(
        [(1, "Иванов Сергей Петрович"),
        (2, "Папандопуло Александр"),
        (3, "Красный велосипед")]);

    ApiError? buildError = await sut.BuildAsync(buildRequest);

    // Act
    SearchQueryResponse? searchResult = sut.Search(
        new SearchQueryRequest
        {
          Query = "веласипед",
          SearchType = SearchType.NearSearch,
          SearchLocation = SearchLocation.BeginWord,
          PrecisionSearch = 70
        },
        out ApiError? searchError);

    // Assert
    Assert.Null(buildError);
    Assert.Null(searchError);
    Assert.NotNull(searchResult);
    Assert.False(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
    Assert.True(ContainsId(searchResult, 3));
  }

  /// <summary>
  /// Проверяет, что построение индекса без документов возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task BuildAsync_БезДокументов_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest request = new()
    {
      IsPhoneticSearch = false,
      Documents = []
    };

    // Act
    ApiError? error = await sut.BuildAsync(request);
    IndexStatusResponse status = sut.GetStatus();

    // Assert
    Assert.NotNull(error);
    Assert.Equal("EmptyDocuments", error.Code);

    Assert.False(status.IsReady);
    Assert.Equal(0, status.DocumentCount);
    Assert.Equal(0, status.SearchableDocumentCount);
  }

  /// <summary>
  /// Проверяет, что построение индекса без пригодного текста возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task BuildAsync_БезПригодногоТекста_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest request = new()
    {
      IsPhoneticSearch = false,
      Documents =
        [
            new()
            {
                Id = 1,
                Text = string.Empty
            },
            new()
            {
                Id = 2,
                Text = "   "
            },
            null
        ]
    };

    // Act
    ApiError? error = await sut.BuildAsync(request);
    IndexStatusResponse status = sut.GetStatus();

    // Assert
    Assert.NotNull(error);
    Assert.Equal("EmptySearchableDocuments", error.Code);

    Assert.False(status.IsReady);
    Assert.Equal(0, status.DocumentCount);
    Assert.Equal(0, status.SearchableDocumentCount);
  }

  /// <summary>
  /// Проверяет, что пустая поисковая строка возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public void Search_СПустымЗапросом_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexStore sut = new();

    SearchQueryRequest request = new()
    {
      Query = "   "
    };

    // Act
    SearchQueryResponse? result = sut.Search(request, out ApiError? error);

    // Assert
    Assert.Null(result);
    Assert.NotNull(error);
    Assert.Equal("EmptyQuery", error.Code);
  }

  /// <summary>
  /// Проверяет, что ошибка повторного построения индекса не сбрасывает уже опубликованный индекс.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ПриОшибкеПовторногоПостроения_НеСбрасываетТекущийИндекс()
  {
    // Arrange
    SearchIndexStore sut = new();

    IndexBuildRequest validRequest = CreateBuildRequest(
        [(1, "Иванов Сергей Петрович")]);

    IndexBuildRequest invalidRequest = new()
    {
      IsPhoneticSearch = false,
      Documents =
        [
            new()
            {
                Id = 2,
                Text = string.Empty
            }
        ]
    };

    // Act
    ApiError? validBuildError = await sut.BuildAsync(validRequest);
    ApiError? invalidBuildError = await sut.BuildAsync(invalidRequest);

    IndexStatusResponse status = sut.GetStatus();

    SearchQueryResponse? searchResult = sut.Search(
        new SearchQueryRequest
        {
          Query = "Иванов"
        },
        out ApiError? searchError);

    // Assert
    Assert.Null(validBuildError);

    Assert.NotNull(invalidBuildError);
    Assert.Equal("EmptySearchableDocuments", invalidBuildError.Code);

    Assert.True(status.IsReady);
    Assert.Equal(1, status.DocumentCount);
    Assert.Equal(1, status.SearchableDocumentCount);

    Assert.Null(searchError);
    Assert.NotNull(searchResult);
    Assert.True(ContainsId(searchResult, 1));
  }

  /// <summary>
  /// Проверяет, что превышение максимального количества документов возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ПриПревышенииКоличестваДокументов_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexStore sut = new(
        Options.Create(new SearchEngineServiceOptions
        {
          MaxDocumentCount = 1,
          MaxDocumentTextLength = 10_000
        }));

    IndexBuildRequest request = CreateBuildRequest(
        [(1, "Иванов Сергей Петрович"),
        (2, "Папандопуло Александр")]);

    // Act
    ApiError? error = await sut.BuildAsync(request);
    IndexStatusResponse status = sut.GetStatus();

    // Assert
    Assert.NotNull(error);
    Assert.Equal("TooManyDocuments", error.Code);

    Assert.False(status.IsReady);
    Assert.Equal(0, status.DocumentCount);
    Assert.Equal(0, status.SearchableDocumentCount);
  }

  /// <summary>
  /// Проверяет, что слишком длинный текст документа возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ПриСлишкомДлинномТекстеДокумента_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexStore sut = new(
        Options.Create(new SearchEngineServiceOptions
        {
          MaxDocumentCount = 100,
          MaxDocumentTextLength = 5
        }));

    IndexBuildRequest request = CreateBuildRequest(
        [(1, "Очень длинный текст")]);

    // Act
    ApiError? error = await sut.BuildAsync(request);
    IndexStatusResponse status = sut.GetStatus();

    // Assert
    Assert.NotNull(error);
    Assert.Equal("DocumentTextTooLong", error.Code);

    Assert.False(status.IsReady);
    Assert.Equal(0, status.DocumentCount);
    Assert.Equal(0, status.SearchableDocumentCount);
  }

  /// <summary>
  /// Проверяет, что после успешного построения индекса сохраняется snapshot-файл.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ПриВключенномSnapshot_СохраняетSnapshotФайл()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      SearchEngineServiceOptions options = new()
      {
        Snapshot = new SearchIndexSnapshotOptions
        {
          IsEnabled = true,
          FilePath = filePath
        }
      };

      SearchIndexSnapshotStorage snapshotStorage = new(
          Options.Create(options));

      SearchIndexStore sut = new(
          Options.Create(options),
          snapshotStorage);

      IndexBuildRequest request = CreateBuildRequest(
          isPhoneticSearch: true,
          [(1, "Иванов Сергей Петрович"),
          (2, "Папандопуло Александр")]);

      // Act
      ApiError? error = await sut.BuildAsync(request);

      SearchIndexSnapshotFile? snapshot =
          await snapshotStorage.LoadAsync();

      // Assert
      Assert.Null(error);

      Assert.NotNull(snapshot);
      Assert.Equal(1, snapshot.Version);
      Assert.True(snapshot.IsPhoneticSearch);
      Assert.Equal(2, snapshot.Documents.Count);

      Assert.Equal(1, snapshot.Documents[0].Id);
      Assert.Equal("Иванов Сергей Петрович", snapshot.Documents[0].Text);

      Assert.Equal(2, snapshot.Documents[1].Id);
      Assert.Equal("Папандопуло Александр", snapshot.Documents[1].Text);
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Проверяет, что восстановление индекса при выключенном snapshot возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task RestoreAsync_ПриВыключенномSnapshot_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexStore sut = new();

    // Act
    ApiError? error = await sut.RestoreAsync();

    IndexStatusResponse status = sut.GetStatus();

    // Assert
    Assert.NotNull(error);
    Assert.Equal("SnapshotDisabled", error.Code);

    Assert.False(status.IsReady);
    Assert.Equal(0, status.DocumentCount);
    Assert.Equal(0, status.SearchableDocumentCount);
  }

  /// <summary>
  /// Проверяет, что восстановление индекса при отсутствующем snapshot-файле возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task RestoreAsync_ПриОтсутствующемSnapshot_ВозвращаетОшибку()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      SearchEngineServiceOptions options = new()
      {
        Snapshot = new SearchIndexSnapshotOptions
        {
          IsEnabled = true,
          FilePath = filePath
        }
      };

      SearchIndexSnapshotStorage snapshotStorage = new(
          Options.Create(options));

      SearchIndexStore sut = new(
          Options.Create(options),
          snapshotStorage);

      // Act
      ApiError? error = await sut.RestoreAsync();

      IndexStatusResponse status = sut.GetStatus();

      // Assert
      Assert.NotNull(error);
      Assert.Equal("SnapshotNotFound", error.Code);

      Assert.False(status.IsReady);
      Assert.Equal(0, status.DocumentCount);
      Assert.Equal(0, status.SearchableDocumentCount);
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Проверяет, что индекс восстанавливается из существующего snapshot-файла.
  /// </summary>
  [Fact]
  public async Task RestoreAsync_ПриСуществующемSnapshot_ВосстанавливаетИндекс()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      DateTimeOffset createdAtUtc = new(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);

      SearchEngineServiceOptions options = new()
      {
        Snapshot = new SearchIndexSnapshotOptions
        {
          IsEnabled = true,
          FilePath = filePath
        }
      };

      SearchIndexSnapshotStorage snapshotStorage = new(
          Options.Create(options));

      SearchIndexSnapshotFile snapshotFile = new()
      {
        Version = 1,
        IsPhoneticSearch = true,
        CreatedAtUtc = createdAtUtc,
        Documents =
          [
              new()
                {
                    Id = 1,
                    Text = "Иванов Сергей Петрович"
                },
                new()
                {
                    Id = 2,
                    Text = "Папандопуло Александр"
                }
          ]
      };

      await snapshotStorage.SaveAsync(snapshotFile);

      SearchIndexStore sut = new(
          Options.Create(options),
          snapshotStorage);

      // Act
      ApiError? restoreError = await sut.RestoreAsync();

      IndexStatusResponse status = sut.GetStatus();

      SearchQueryResponse? searchResult = sut.Search(
          new SearchQueryRequest
          {
            Query = "Ivanov"
          },
          out ApiError? searchError);

      // Assert
      Assert.Null(restoreError);

      Assert.True(status.IsReady);
      Assert.Equal(2, status.DocumentCount);
      Assert.Equal(2, status.SearchableDocumentCount);
      Assert.True(status.IsPhoneticSearch);
      Assert.Equal(createdAtUtc, status.CreatedAtUtc);

      Assert.Null(searchError);
      Assert.NotNull(searchResult);
      Assert.True(ContainsId(searchResult, 1));
      Assert.False(ContainsId(searchResult, 2));
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Создаёт временный путь к snapshot-файлу.
  /// </summary>
  /// <returns>Временный путь к snapshot-файлу.</returns>
  private static string CreateTempSnapshotPath()
  {
    return Path.Combine(
        Path.GetTempPath(),
        "SearchEngine.Service.Tests",
        Guid.NewGuid().ToString("N"),
        "search-index-snapshot.json");
  }

  /// <summary>
  /// Удаляет временную папку snapshot-файла.
  /// </summary>
  /// <param name="filePath">Путь к snapshot-файлу.</param>
  private static void DeleteTempSnapshotDirectory(string filePath)
  {
    string? directoryPath = Path.GetDirectoryName(filePath);

    if (string.IsNullOrWhiteSpace(directoryPath))
      return;

    if (Directory.Exists(directoryPath))
      Directory.Delete(directoryPath, recursive: true);
  }

  /// <summary>
  /// Создаёт запрос на построение индекса.
  /// </summary>
  /// <param name="documents">Документы для индексации.</param>
  /// <returns>Запрос на построение индекса.</returns>
  private static IndexBuildRequest CreateBuildRequest(params (int Id, string Text)[] documents) => CreateBuildRequest(false, documents);

  /// <summary>
  /// Создаёт запрос на построение индекса.
  /// </summary>
  /// <param name="isPhoneticSearch">Признак включения фонетического поиска.</param>
  /// <param name="documents">Документы для индексации.</param>
  /// <returns>Запрос на построение индекса.</returns>
  private static IndexBuildRequest CreateBuildRequest(
      bool isPhoneticSearch,
      params (int Id, string Text)[] documents)
  {
    return new IndexBuildRequest
    {
      IsPhoneticSearch = isPhoneticSearch,
      Documents = [.. documents
            .Select(document => new IndexDocumentRequest
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