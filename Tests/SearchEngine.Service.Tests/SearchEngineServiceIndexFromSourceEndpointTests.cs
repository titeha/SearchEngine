using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Интеграционные тесты построения индекса из источника данных.
/// </summary>
public sealed class SearchEngineServiceIndexFromSourceEndpointTests
{
  /// <summary>
  /// Проверяет, что пустое имя источника данных возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СПустымИменемИсточника_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      sourceName = " ",
      isPhoneticSearch = true
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index/from-source", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("EmptySourceName", error.Code);
  }

  /// <summary>
  /// Проверяет, что неизвестный источник данных возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СНеизвестнымИсточником_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      sourceName = "products",
      isPhoneticSearch = true
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index/from-source", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("DataSourceNotFound", error.Code);
  }

  /// <summary>
  /// Проверяет, что отключённый источник данных возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СОтключеннымИсточником_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithDataSource(
        isEnabled: false,
        provider: "test",
        registerReader: true);

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      sourceName = "products",
      isPhoneticSearch = true
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index/from-source", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("DataSourceDisabled", error.Code);
  }

  /// <summary>
  /// Проверяет, что неподдерживаемый provider источника данных возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СНеподдерживаемымProvider_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithDataSource(
        isEnabled: true,
        provider: "postgres",
        registerReader: false);

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      sourceName = "products",
      isPhoneticSearch = true
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index/from-source", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("DataSourceProviderNotSupported", error.Code);
  }

  /// <summary>
  /// Проверяет, что индекс строится из документов, прочитанных provider-ом источника данных.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СПрофилемИReader_СтроитИндекс()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithDataSource(
        isEnabled: true,
        provider: "test",
        registerReader: true);

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      sourceName = "products",
      isPhoneticSearch = true
    };

    // Act
    HttpResponseMessage buildResponse = await client.PostAsJsonAsync("/v1/index/from-source", request);

    IndexStatusResponse? status =
        await buildResponse.Content.ReadFromJsonAsync<IndexStatusResponse>();

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
    Assert.Equal(HttpStatusCode.OK, buildResponse.StatusCode);

    Assert.NotNull(status);
    Assert.True(status.IsReady);
    Assert.Equal(2, status.DocumentCount);
    Assert.Equal(2, status.SearchableDocumentCount);
    Assert.True(status.IsPhoneticSearch);

    Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

    Assert.NotNull(searchResult);
    Assert.True(searchResult.IsHasIndex);
    Assert.True(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
  }

  /// <summary>
  /// Создаёт фабрику приложения с тестовым источником данных.
  /// </summary>
  /// <param name="isEnabled">Признак включения источника данных.</param>
  /// <param name="provider">Provider источника данных.</param>
  /// <param name="registerReader">Признак регистрации тестового reader-а.</param>
  /// <returns>Фабрика тестового приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactoryWithDataSource(
      bool isEnabled,
      string provider,
      bool registerReader)
  {
    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
          builder.ConfigureAppConfiguration((_, configuration) =>
          {
            configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                      ["SearchEngineService:Sources:products:IsEnabled"] = isEnabled.ToString(),
                      ["SearchEngineService:Sources:products:Provider"] = provider,
                      ["SearchEngineService:Sources:products:ConnectionStringName"] = "PRODUCTS_DB",
                      ["SearchEngineService:Sources:products:Query"] = "select id, name as text from products"
                    });
          });

          if (registerReader)
            builder.ConfigureServices(services => services.AddSingleton<ISearchDataSourceReader, TestSearchDataSourceReader>());
        });
  }

  /// <summary>
  /// Проверяет наличие идентификатора документа в ответе поиска.
  /// </summary>
  /// <param name="response">Ответ поиска.</param>
  /// <param name="id">Идентификатор документа.</param>
  /// <returns><see langword="true"/>, если идентификатор найден.</returns>
  private static bool ContainsId(SearchQueryResponse response, int id) => response.Items.Any(bucket => bucket.Ids.Contains(id));

  /// <summary>
  /// Тестовый reader источника данных.
  /// </summary>
  private sealed class TestSearchDataSourceReader : ISearchDataSourceReader
  {
    /// <inheritdoc />
    public string Provider => "test";

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchDataSourceDocument>> ReadAsync(
        string sourceName,
        SearchDataSourceOptions options,
        CancellationToken cancellationToken = default)
    {
      IReadOnlyList<SearchDataSourceDocument> documents =
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
      ];

      return Task.FromResult(documents);
    }
  }
}