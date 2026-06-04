using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
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
        provider: "oracle",
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
  /// Проверяет, что индекс строится из встроенного in-memory источника данных.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СInMemoryProvider_СтроитИндекс()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithInMemoryDataSource();

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      sourceName = "demo",
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
  /// Проверяет, что индекс строится из SQLite-источника данных.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СSqliteProvider_СтроитИндекс()
  {
    // Arrange
    string databasePath = CreateTempSqlitePath();

    try
    {
      await CreateSqliteDatabaseAsync(databasePath);

      await using WebApplicationFactory<Program> factory =
          CreateFactoryWithSqliteDataSource(databasePath);

      using HttpClient client = factory.CreateClient();

      object request = new
      {
        sourceName = "sqlite-demo",
        isPhoneticSearch = true
      };

      // Act
      HttpResponseMessage buildResponse = await client.PostAsJsonAsync(
          "/v1/index/from-source",
          request);

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
      Assert.Equal(3, status.DocumentCount);
      Assert.Equal(3, status.SearchableDocumentCount);
      Assert.True(status.IsPhoneticSearch);

      Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

      Assert.NotNull(searchResult);
      Assert.True(searchResult.IsHasIndex);
      Assert.True(ContainsId(searchResult, 1));
      Assert.False(ContainsId(searchResult, 2));
      Assert.False(ContainsId(searchResult, 3));
    }
    finally
    {
      DeleteTempSqliteDirectory(databasePath);
    }
  }

  /// <summary>
  /// Проверяет, что источник данных без provider-а возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СПустымProvider_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithDataSource(
        isEnabled: true,
        provider: " ",
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
    Assert.Equal("DataSourceProviderIsEmpty", error.Code);
  }

  /// <summary>
  /// Проверяет, что SQLite-источник без имени строки подключения возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СSqliteProviderБезConnectionStringName_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithSqliteProfile(
        connectionStringName: " ",
        query: "select id, text from search_documents");

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      sourceName = "sqlite-demo",
      isPhoneticSearch = true
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index/from-source", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("DataSourceConnectionStringNameIsEmpty", error.Code);
  }

  /// <summary>
  /// Проверяет, что SQLite-источник без SQL-запроса возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СSqliteProviderБезQuery_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithSqliteProfile(
        connectionStringName: "SQLITE_DEMO",
        query: " ");

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      sourceName = "sqlite-demo",
      isPhoneticSearch = true
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index/from-source", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("DataSourceQueryIsEmpty", error.Code);
  }

  /// <summary>
  /// Проверяет, что PostgreSQL-источник без строки подключения возвращает ошибку чтения источника.
  /// </summary>
  [Fact]
  public async Task PostIndexFromSource_СPostgresProviderБезConnectionString_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithPostgresDataSourceWithoutConnectionString();

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
    Assert.Equal("DataSourceReadFailed", error.Code);
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
  /// Создаёт фабрику приложения с PostgreSQL-источником без строки подключения.
  /// </summary>
  /// <returns>Фабрика тестового приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactoryWithPostgresDataSourceWithoutConnectionString()
  {
    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
          builder.ConfigureAppConfiguration((_, configuration) =>
          {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                  ["SearchEngineService:Sources:products:IsEnabled"] = "true",
                  ["SearchEngineService:Sources:products:Provider"] = "postgres",
                  ["SearchEngineService:Sources:products:ConnectionStringName"] = "POSTGRES_DEMO",
                  ["SearchEngineService:Sources:products:Query"] =
                        "select id, text from search_documents"
                });
          });
        });
  }

  /// <summary>
  /// Создаёт фабрику приложения с SQLite-источником данных.
  /// </summary>
  /// <param name="databasePath">Путь к SQLite-файлу.</param>
  /// <returns>Фабрика тестового приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactoryWithSqliteDataSource(
      string databasePath)
  {
    string connectionString = CreateSqliteConnectionString(databasePath);

    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
          builder.ConfigureAppConfiguration((_, configuration) =>
          {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                  ["ConnectionStrings:SQLITE_DEMO"] = connectionString,

                  ["SearchEngineService:Sources:sqlite-demo:IsEnabled"] = "true",
                  ["SearchEngineService:Sources:sqlite-demo:Provider"] = "sqlite",
                  ["SearchEngineService:Sources:sqlite-demo:ConnectionStringName"] = "SQLITE_DEMO",
                  ["SearchEngineService:Sources:sqlite-demo:Query"] =
                        "select id, text from search_documents order by id"
                });
          });
        });
  }

  /// <summary>
  /// Создаёт временную SQLite-БД с тестовыми документами.
  /// </summary>
  /// <param name="databasePath">Путь к SQLite-файлу.</param>
  private static async Task CreateSqliteDatabaseAsync(string databasePath)
  {
    string? directoryPath = Path.GetDirectoryName(databasePath);

    if (!string.IsNullOrWhiteSpace(directoryPath))
      Directory.CreateDirectory(directoryPath);

    await using SqliteConnection connection = new(CreateSqliteConnectionString(databasePath));

    await connection.OpenAsync();

    await using SqliteCommand createCommand = connection.CreateCommand();

    createCommand.CommandText = """
        create table search_documents
        (
            id integer not null primary key,
            text text not null
        );
        """;

    await createCommand.ExecuteNonQueryAsync();

    await using SqliteCommand insertCommand = connection.CreateCommand();

    insertCommand.CommandText = """
        insert into search_documents (id, text)
        values
            (1, 'Иванов Сергей Петрович'),
            (2, 'Папандопуло Александр'),
            (3, 'Красный велосипед');
        """;

    await insertCommand.ExecuteNonQueryAsync();
  }

  /// <summary>
  /// Создаёт временный путь к SQLite-файлу.
  /// </summary>
  /// <returns>Временный путь к SQLite-файлу.</returns>
  private static string CreateTempSqlitePath()
  {
    return Path.Combine(
        Path.GetTempPath(),
        "SearchEngine.Service.Tests",
        Guid.NewGuid().ToString("N"),
        "search-demo.db");
  }

  /// <summary>
  /// Удаляет временную папку SQLite-файла.
  /// </summary>
  /// <param name="databasePath">Путь к SQLite-файлу.</param>
  private static void DeleteTempSqliteDirectory(string databasePath)
  {
    SqliteConnection.ClearAllPools();

    string? directoryPath = Path.GetDirectoryName(databasePath);

    if (string.IsNullOrWhiteSpace(directoryPath))
      return;

    if (!Directory.Exists(directoryPath))
      return;

    for (int attempt = 0; attempt < 5; attempt++)
      try
      {
        Directory.Delete(directoryPath, recursive: true);
        return;
      }
      catch (IOException) when (attempt < 4)
      {
        Thread.Sleep(100);
      }
      catch (UnauthorizedAccessException) when (attempt < 4)
      {
        Thread.Sleep(100);
      }

    Directory.Delete(directoryPath, recursive: true);
  }

  /// <summary>
  /// Создаёт фабрику приложения с SQLite-профилем источника данных.
  /// </summary>
  /// <param name="connectionStringName">Имя строки подключения.</param>
  /// <param name="query">SQL-запрос источника данных.</param>
  /// <returns>Фабрика тестового приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactoryWithSqliteProfile(
      string? connectionStringName,
      string? query)
  {
    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
          builder.ConfigureAppConfiguration((_, configuration) =>
          {
            Dictionary<string, string?> values = new()
            {
              ["SearchEngineService:Sources:sqlite-demo:IsEnabled"] = "true",
              ["SearchEngineService:Sources:sqlite-demo:Provider"] = "sqlite"
            };

            if (connectionStringName is not null)
            {
              values["SearchEngineService:Sources:sqlite-demo:ConnectionStringName"] =
                  connectionStringName;
            }

            if (query is not null)
            {
              values["SearchEngineService:Sources:sqlite-demo:Query"] =
                  query;
            }

            configuration.AddInMemoryCollection(values);
          });
        });
  }

  /// <summary>
  /// Создаёт фабрику приложения с in-memory источником данных.
  /// </summary>
  /// <returns>Фабрика тестового приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactoryWithInMemoryDataSource()
  {
    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
          builder.ConfigureAppConfiguration((_, configuration) =>
          {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                  ["SearchEngineService:Sources:demo:IsEnabled"] = "true",
                  ["SearchEngineService:Sources:demo:Provider"] = "in-memory",

                  ["SearchEngineService:Sources:demo:Documents:0:Id"] = "1",
                  ["SearchEngineService:Sources:demo:Documents:0:Text"] = "Иванов Сергей Петрович",

                  ["SearchEngineService:Sources:demo:Documents:1:Id"] = "2",
                  ["SearchEngineService:Sources:demo:Documents:1:Text"] = "Папандопуло Александр"
                });
          });
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

  /// <summary>
  /// Создаёт строку подключения к тестовой SQLite-БД.
  /// </summary>
  /// <param name="databasePath">Путь к SQLite-файлу.</param>
  /// <returns>Строка подключения SQLite.</returns>
  private static string CreateSqliteConnectionString(string databasePath)
  {
    SqliteConnectionStringBuilder builder = new()
    {
      DataSource = databasePath,
      Pooling = false
    };

    return builder.ToString();
  }
}