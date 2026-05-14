using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Интеграционные тесты HTTP-endpoint-ов поискового индекса.
/// </summary>
public sealed class SearchEngineServiceIndexEndpointTests
{
  /// <summary>
  /// Проверяет, что до построения индекса endpoint состояния возвращает пустой индекс.
  /// </summary>
  [Fact]
  public async Task GetIndex_ДоПостроенияИндекса_ВозвращаетПустоеСостояние()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    // Act
    IndexStatusResponse? response = await client.GetFromJsonAsync<IndexStatusResponse>("/v1/index");

    // Assert
    Assert.NotNull(response);
    Assert.False(response.IsReady);
    Assert.Equal(0, response.DocumentCount);
    Assert.Equal(0, response.SearchableDocumentCount);
    Assert.False(response.IsPhoneticSearch);
    Assert.Null(response.CreatedAtUtc);
  }

  /// <summary>
  /// Проверяет, что endpoint построения индекса создаёт индекс и обновляет состояние.
  /// </summary>
  [Fact]
  public async Task PostIndex_СКорректнымиДокументами_СтроитИндекс()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    IndexBuildRequest request = new()
    {
      IsPhoneticSearch = true,
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
                },
                new()
                {
                    Id = 3,
                    Text = "Красный велосипед"
                }
        ]
    };

    // Act
    HttpResponseMessage buildResponse = await client.PostAsJsonAsync("/v1/index", request);

    IndexStatusResponse? buildStatus =
        await buildResponse.Content.ReadFromJsonAsync<IndexStatusResponse>();

    IndexStatusResponse? currentStatus =
        await client.GetFromJsonAsync<IndexStatusResponse>("/v1/index");

    // Assert
    Assert.Equal(HttpStatusCode.OK, buildResponse.StatusCode);

    Assert.NotNull(buildStatus);
    Assert.True(buildStatus.IsReady);
    Assert.Equal(3, buildStatus.DocumentCount);
    Assert.Equal(3, buildStatus.SearchableDocumentCount);
    Assert.True(buildStatus.IsPhoneticSearch);
    Assert.NotNull(buildStatus.CreatedAtUtc);

    Assert.NotNull(currentStatus);
    Assert.True(currentStatus.IsReady);
    Assert.Equal(3, currentStatus.DocumentCount);
    Assert.Equal(3, currentStatus.SearchableDocumentCount);
    Assert.True(currentStatus.IsPhoneticSearch);
    Assert.NotNull(currentStatus.CreatedAtUtc);
  }

  /// <summary>
  /// Проверяет, что endpoint построения индекса возвращает ошибку при пустом списке документов.
  /// </summary>
  [Fact]
  public async Task PostIndex_БезДокументов_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    IndexBuildRequest request = new()
    {
      IsPhoneticSearch = false,
      Documents = []
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    Assert.NotNull(error);
    Assert.Equal("EmptyDocuments", error.Code);
  }

  /// <summary>
  /// Проверяет, что endpoint построения индекса возвращает ошибку при отсутствии пригодного текста.
  /// </summary>
  [Fact]
  public async Task PostIndex_БезПригодногоТекста_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

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
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    Assert.NotNull(error);
    Assert.Equal("EmptySearchableDocuments", error.Code);
  }

  /// <summary>
  /// Проверяет, что endpoint готовности до построения индекса возвращает 503.
  /// </summary>
  [Fact]
  public async Task GetReady_ДоПостроенияИндекса_ВозвращаетServiceUnavailable()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.GetAsync("/ready");

    ReadinessResponse? readiness =
        await response.Content.ReadFromJsonAsync<ReadinessResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

    Assert.NotNull(readiness);
    Assert.Equal("not_ready", readiness.Status);
    Assert.False(readiness.IsReady);
    Assert.Equal(0, readiness.DocumentCount);
    Assert.Equal(0, readiness.SearchableDocumentCount);
    Assert.False(readiness.IsPhoneticSearch);
    Assert.Null(readiness.CreatedAtUtc);
  }

  /// <summary>
  /// Проверяет, что endpoint готовности после построения индекса возвращает 200.
  /// </summary>
  [Fact]
  public async Task GetReady_ПослеПостроенияИндекса_ВозвращаетOk()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    IndexBuildRequest request = new()
    {
      IsPhoneticSearch = true,
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
            },
            new()
            {
                Id = 3,
                Text = "Красный велосипед"
            }
        ]
    };

    HttpResponseMessage buildResponse = await client.PostAsJsonAsync("/v1/index", request);

    buildResponse.EnsureSuccessStatusCode();

    // Act
    HttpResponseMessage response = await client.GetAsync("/ready");

    ReadinessResponse? readiness =
        await response.Content.ReadFromJsonAsync<ReadinessResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.NotNull(readiness);
    Assert.Equal("ready", readiness.Status);
    Assert.True(readiness.IsReady);
    Assert.Equal(3, readiness.DocumentCount);
    Assert.Equal(3, readiness.SearchableDocumentCount);
    Assert.True(readiness.IsPhoneticSearch);
    Assert.NotNull(readiness.CreatedAtUtc);
  }

  /// <summary>
  /// Проверяет, что endpoint построения индекса возвращает ошибку при превышении количества документов.
  /// </summary>
  [Fact]
  public async Task PostIndex_ПриПревышенииКоличестваДокументов_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithLimits(
        maxDocumentCount: 1,
        maxDocumentTextLength: 10_000);

    using HttpClient client = factory.CreateClient();

    IndexBuildRequest request = new()
    {
      IsPhoneticSearch = false,
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

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    IndexStatusResponse? status =
        await client.GetFromJsonAsync<IndexStatusResponse>("/v1/index");

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    Assert.NotNull(error);
    Assert.Equal("TooManyDocuments", error.Code);

    Assert.NotNull(status);
    Assert.False(status.IsReady);
    Assert.Equal(0, status.DocumentCount);
    Assert.Equal(0, status.SearchableDocumentCount);
  }

  /// <summary>
  /// Проверяет, что endpoint построения индекса возвращает ошибку при слишком длинном тексте документа.
  /// </summary>
  [Fact]
  public async Task PostIndex_ПриСлишкомДлинномТекстеДокумента_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithLimits(
        maxDocumentCount: 100,
        maxDocumentTextLength: 5);

    using HttpClient client = factory.CreateClient();

    IndexBuildRequest request = new()
    {
      IsPhoneticSearch = false,
      Documents =
        [
            new()
            {
                Id = 1,
                Text = "Очень длинный текст"
            }
        ]
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    IndexStatusResponse? status =
        await client.GetFromJsonAsync<IndexStatusResponse>("/v1/index");

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    Assert.NotNull(error);
    Assert.Equal("DocumentTextTooLong", error.Code);

    Assert.NotNull(status);
    Assert.False(status.IsReady);
    Assert.Equal(0, status.DocumentCount);
    Assert.Equal(0, status.SearchableDocumentCount);
  }

  /// <summary>
  /// Проверяет, что endpoint конфигурации возвращает переопределённые настройки сервиса.
  /// </summary>
  [Fact]
  public async Task GetConfig_СПереопределеннымиНастройками_ВозвращаетАктивныеОграничения()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithLimits(
        maxDocumentCount: 1,
        maxDocumentTextLength: 5);

    using HttpClient client = factory.CreateClient();

    // Act
    SearchEngineServiceConfigResponse? response =
        await client.GetFromJsonAsync<SearchEngineServiceConfigResponse>("/v1/config");

    // Assert
    Assert.NotNull(response);
    Assert.Equal(1, response.MaxDocumentCount);
    Assert.Equal(5, response.MaxDocumentTextLength);
    Assert.NotNull(response.Snapshot);
    Assert.True(response.Snapshot.IsEnabled);
    Assert.Equal("data/test-snapshot.json", response.Snapshot.FilePath);
  }

  /// <summary>
  /// Проверяет, что endpoint построения индекса сохраняет snapshot-файл при включённой настройке snapshot.
  /// </summary>
  [Fact]
  public async Task PostIndex_ПриВключенномSnapshot_СохраняетSnapshotФайл()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      await using WebApplicationFactory<Program> factory =
          CreateFactoryWithSnapshot(filePath);

      using HttpClient client = factory.CreateClient();

      IndexBuildRequest request = new()
      {
        IsPhoneticSearch = true,
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

      // Act
      HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", request);

      // Assert
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.True(File.Exists(filePath));

      string json = await File.ReadAllTextAsync(filePath);

      Assert.Contains("Иванов Сергей Петрович", json);
      Assert.Contains("Папандопуло Александр", json);
      Assert.DoesNotContain("\\u0418", json);

      SearchIndexSnapshotFile? snapshot = JsonSerializer.Deserialize<SearchIndexSnapshotFile>(
          json,
          new JsonSerializerOptions(JsonSerializerDefaults.Web));

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
  /// Проверяет, что endpoint восстановления индекса возвращает ошибку при выключенном snapshot.
  /// </summary>
  [Fact]
  public async Task PostIndexRestore_ПриВыключенномSnapshot_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.PostAsync("/v1/index/restore", content: null);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    IndexStatusResponse? status =
        await client.GetFromJsonAsync<IndexStatusResponse>("/v1/index");

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    Assert.NotNull(error);
    Assert.Equal("SnapshotDisabled", error.Code);

    Assert.NotNull(status);
    Assert.False(status.IsReady);
    Assert.Equal(0, status.DocumentCount);
    Assert.Equal(0, status.SearchableDocumentCount);
  }

  /// <summary>
  /// Проверяет, что endpoint восстановления индекса возвращает ошибку при отсутствующем snapshot-файле.
  /// </summary>
  [Fact]
  public async Task PostIndexRestore_ПриОтсутствующемSnapshot_ВозвращаетBadRequest()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      await using WebApplicationFactory<Program> factory =
          CreateFactoryWithSnapshot(filePath);

      using HttpClient client = factory.CreateClient();

      // Act
      HttpResponseMessage response = await client.PostAsync("/v1/index/restore", content: null);

      ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

      IndexStatusResponse? status =
          await client.GetFromJsonAsync<IndexStatusResponse>("/v1/index");

      // Assert
      Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

      Assert.NotNull(error);
      Assert.Equal("SnapshotNotFound", error.Code);

      Assert.NotNull(status);
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
  /// Проверяет, что endpoint восстановления индекса восстанавливает индекс из snapshot-файла.
  /// </summary>
  [Fact]
  public async Task PostIndexRestore_ПриСуществующемSnapshot_ВосстанавливаетИндекс()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      DateTimeOffset createdAtUtc = new(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);

      await SaveSnapshotAsync(filePath, createdAtUtc);

      await using WebApplicationFactory<Program> factory =
          CreateFactoryWithSnapshot(filePath);

      using HttpClient client = factory.CreateClient();

      // Act
      HttpResponseMessage restoreResponse = await client.PostAsync("/v1/index/restore", content: null);

      IndexStatusResponse? status =
          await restoreResponse.Content.ReadFromJsonAsync<IndexStatusResponse>();

      HttpResponseMessage searchResponse = await client.PostAsJsonAsync(
          "/v1/search",
          new
          {
            query = "Ivanov",
            matchMode = "AllTerms",
            searchType = "ExactSearch",
            searchLocation = "BeginWord"
          });

      SearchQueryResponse? searchResult =
          await searchResponse.Content.ReadFromJsonAsync<SearchQueryResponse>();

      // Assert
      Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

      Assert.NotNull(status);
      Assert.True(status.IsReady);
      Assert.Equal(2, status.DocumentCount);
      Assert.Equal(2, status.SearchableDocumentCount);
      Assert.True(status.IsPhoneticSearch);
      Assert.Equal(createdAtUtc, status.CreatedAtUtc);

      Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

      Assert.NotNull(searchResult);
      Assert.True(searchResult.IsHasIndex);
      Assert.True(ContainsId(searchResult, 1));
      Assert.False(ContainsId(searchResult, 2));
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Проверяет, что сервис автоматически восстанавливает индекс из snapshot при старте.
  /// </summary>
  [Fact]
  public async Task StartAsync_ПриВключенномAutoRestore_ВосстанавливаетИндексИзSnapshot()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      DateTimeOffset createdAtUtc = new(2026, 5, 12, 10, 0, 0, TimeSpan.Zero);

      await SaveSnapshotAsync(filePath, createdAtUtc);

      await using WebApplicationFactory<Program> factory =
          CreateFactoryWithAutoRestoreSnapshot(filePath);

      using HttpClient client = factory.CreateClient();

      // Act
      IndexStatusResponse? status =
          await client.GetFromJsonAsync<IndexStatusResponse>("/v1/index");

      HttpResponseMessage readyResponse = await client.GetAsync("/ready");

      HttpResponseMessage searchResponse = await client.PostAsJsonAsync(
          "/v1/search",
          new
          {
            query = "Ivanov",
            matchMode = "AllTerms",
            searchType = "ExactSearch",
            searchLocation = "BeginWord"
          });

      SearchQueryResponse? searchResult =
          await searchResponse.Content.ReadFromJsonAsync<SearchQueryResponse>();

      // Assert
      Assert.NotNull(status);
      Assert.True(status.IsReady);
      Assert.Equal(2, status.DocumentCount);
      Assert.Equal(2, status.SearchableDocumentCount);
      Assert.True(status.IsPhoneticSearch);
      Assert.Equal(createdAtUtc, status.CreatedAtUtc);

      Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
      Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

      Assert.NotNull(searchResult);
      Assert.True(searchResult.IsHasIndex);
      Assert.True(ContainsId(searchResult, 1));
      Assert.False(ContainsId(searchResult, 2));
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Сохраняет тестовый snapshot-файл поискового индекса.
  /// </summary>
  /// <param name="filePath">Путь к snapshot-файлу.</param>
  /// <param name="createdAtUtc">Дата и время создания snapshot-файла в UTC.</param>
  private static async Task SaveSnapshotAsync(
      string filePath,
      DateTimeOffset createdAtUtc)
  {
    SearchEngineServiceOptions options = new()
    {
      Snapshot = new SearchIndexSnapshotOptions
      {
        IsEnabled = true,
        FilePath = filePath
      }
    };

    SearchIndexSnapshotStorage storage = new(
        Options.Create(options));

    SearchIndexSnapshotFile snapshot = new()
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

    await storage.SaveAsync(snapshot);
  }

  /// <summary>
  /// Проверяет наличие идентификатора документа в ответе поиска.
  /// </summary>
  /// <param name="response">Ответ поиска.</param>
  /// <param name="id">Идентификатор документа.</param>
  /// <returns><see langword="true"/>, если идентификатор найден.</returns>
  private static bool ContainsId(SearchQueryResponse response, int id) => response.Items.Any(bucket => bucket.Ids.Contains(id));

  /// <summary>
  /// Создаёт фабрику приложения с включённым snapshot индекса.
  /// </summary>
  /// <param name="filePath">Путь к snapshot-файлу.</param>
  /// <returns>Фабрика тестового приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactoryWithSnapshot(string filePath)
  {
    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
          builder.ConfigureAppConfiguration((_, configuration) =>
          {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                  ["SearchEngineService:Snapshot:IsEnabled"] = "true",
                  ["SearchEngineService:Snapshot:AutoRestoreOnStart"] = "false",
                  ["SearchEngineService:Snapshot:FilePath"] = filePath
                });
          });
        });
  }
  /// <summary>
  /// Создаёт фабрику приложения с включённым автоматическим восстановлением snapshot индекса.
  /// </summary>
  /// <param name="filePath">Путь к snapshot-файлу.</param>
  /// <returns>Фабрика тестового приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactoryWithAutoRestoreSnapshot(string filePath)
  {
    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
          builder.ConfigureAppConfiguration((_, configuration) =>
          {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                  ["SearchEngineService:Snapshot:IsEnabled"] = "true",
                  ["SearchEngineService:Snapshot:AutoRestoreOnStart"] = "true",
                  ["SearchEngineService:Snapshot:FilePath"] = filePath
                });
          });
        });
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
  /// Создаёт фабрику приложения с тестовыми ограничениями сервиса.
  /// </summary>
  /// <param name="maxDocumentCount">Максимальное количество документов.</param>
  /// <param name="maxDocumentTextLength">Максимальная длина текста одного документа.</param>
  /// <returns>Фабрика тестового приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactoryWithLimits(
      int maxDocumentCount,
      int maxDocumentTextLength)
  {
    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
          builder.ConfigureAppConfiguration((_, configuration) =>
          {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                  ["SearchEngineService:MaxDocumentCount"] =
                        maxDocumentCount.ToString(CultureInfo.InvariantCulture),

                  ["SearchEngineService:MaxDocumentTextLength"] =
                        maxDocumentTextLength.ToString(CultureInfo.InvariantCulture),
                  ["SearchEngineService:Snapshot:IsEnabled"] = "true",
                  ["SearchEngineService:Snapshot:AutoRestoreOnStart"] = "false",
                  ["SearchEngineService:Snapshot:FilePath"] = "data/test-snapshot.json"
                });
          });
        });
  }
}