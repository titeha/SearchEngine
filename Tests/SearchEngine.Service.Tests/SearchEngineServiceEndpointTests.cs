using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Интеграционные тесты HTTP-endpoint-ов поискового сервиса.
/// </summary>
public sealed class SearchEngineServiceEndpointTests
{
  /// <summary>
  /// Проверяет, что endpoint проверки работоспособности возвращает успешный ответ.
  /// </summary>
  [Fact]
  public async Task GetHealth_ДолженВернутьУспешныйОтвет()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    // Act
    HealthResponse? response = await client.GetFromJsonAsync<HealthResponse>("/health");

    // Assert
    Assert.NotNull(response);
    Assert.Equal("ok", response.Status);
  }

  /// <summary>
  /// Проверяет, что endpoint информации о сервисе возвращает сведения о сервисе.
  /// </summary>
  [Fact]
  public async Task GetInfo_ДолженВернутьИнформациюОСервисе()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    // Act
    ServiceInfoResponse? response = await client.GetFromJsonAsync<ServiceInfoResponse>("/v1/info");

    // Assert
    Assert.NotNull(response);
    Assert.Equal("TiSoft.SearchEngine.Service", response.Service);
    Assert.Equal("ok", response.Status);
    Assert.False(string.IsNullOrWhiteSpace(response.SearchEngineVersion));
    Assert.False(string.IsNullOrWhiteSpace(response.ServiceVersion));
    Assert.StartsWith("0.6.0", response.ServiceVersion);
  }

  /// <summary>
  /// Проверяет, что неизвестный endpoint возвращает 404.
  /// </summary>
  [Fact]
  public async Task GetUnknownEndpoint_ДолженВернутьNotFound()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.GetAsync("/unknown");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  /// <summary>
  /// Проверяет, что endpoint справочников поиска возвращает допустимые параметры.
  /// </summary>
  [Fact]
  public async Task GetSearchOptions_ДолженВернутьДопустимыеПараметрыПоиска()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    // Act
    SearchOptionsResponse? response =
        await client.GetFromJsonAsync<SearchOptionsResponse>("/v1/search/options");

    // Assert
    Assert.NotNull(response);

    Assert.Contains("AllTerms", response.MatchModes);
    Assert.Contains("AnyTerm", response.MatchModes);
    Assert.Contains("SoftAllTerms", response.MatchModes);

    Assert.Contains("ExactSearch", response.SearchTypes);
    Assert.Contains("NearSearch", response.SearchTypes);

    Assert.Contains("BeginWord", response.SearchLocations);
    Assert.Contains("InWord", response.SearchLocations);

    Assert.Equal("AllTerms", response.DefaultMatchMode);
    Assert.Equal("ExactSearch", response.DefaultSearchType);
    Assert.Equal("BeginWord", response.DefaultSearchLocation);
  }

  /// <summary>
  /// Проверяет, что endpoint конфигурации возвращает активные настройки сервиса.
  /// </summary>
  [Fact]
  public async Task GetConfig_ДолженВернутьАктивныеНастройкиСервиса()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    // Act
    SearchEngineServiceConfigResponse? response =
        await client.GetFromJsonAsync<SearchEngineServiceConfigResponse>("/v1/config");

    // Assert
    Assert.NotNull(response);
    Assert.Equal(100_000, response.MaxDocumentCount);
    Assert.Equal(10_000, response.MaxDocumentTextLength);
    Assert.NotNull(response.Snapshot);
    Assert.False(response.Snapshot.IsEnabled);
    Assert.False(response.Snapshot.AutoRestoreOnStart);
    Assert.Equal("data/search-index-snapshot.json", response.Snapshot.FilePath);
  }

  /// <summary>
  /// Ответ endpoint-а справочников параметров поиска.
  /// </summary>
  private sealed record SearchOptionsResponse
  {
    /// <summary>
    /// Получает допустимые режимы объединения слов запроса.
    /// </summary>
    public string[] MatchModes { get; init; } = [];

    /// <summary>
    /// Получает допустимые типы поиска.
    /// </summary>
    public string[] SearchTypes { get; init; } = [];

    /// <summary>
    /// Получает допустимые места поиска внутри слова.
    /// </summary>
    public string[] SearchLocations { get; init; } = [];

    /// <summary>
    /// Получает режим объединения слов запроса по умолчанию.
    /// </summary>
    public string? DefaultMatchMode { get; init; }

    /// <summary>
    /// Получает тип поиска по умолчанию.
    /// </summary>
    public string? DefaultSearchType { get; init; }

    /// <summary>
    /// Получает место поиска внутри слова по умолчанию.
    /// </summary>
    public string? DefaultSearchLocation { get; init; }
  }

  /// <summary>
  /// Ответ endpoint-а проверки работоспособности.
  /// </summary>
  private sealed record HealthResponse
  {
    /// <summary>
    /// Получает состояние сервиса.
    /// </summary>
    public string? Status { get; init; }
  }

  /// <summary>
  /// Ответ endpoint-а информации о сервисе.
  /// </summary>
  private sealed record ServiceInfoResponse
  {
    /// <summary>
    /// Получает имя сервиса.
    /// </summary>
    public string? Service { get; init; }

    /// <summary>
    /// Получает состояние сервиса.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Получает версию библиотеки SearchEngine.
    /// </summary>
    public string? SearchEngineVersion { get; init; }

    /// <summary>
    /// Получает версию сервиса.
    /// </summary>
    public string? ServiceVersion { get; init; }
  }

  /// <summary>
  /// Ответ endpoint-а активной конфигурации сервиса.
  /// </summary>
  private sealed record SearchEngineServiceConfigResponse
  {
    /// <summary>
    /// Получает максимальное количество документов для построения индекса.
    /// </summary>
    public int MaxDocumentCount { get; init; }

    /// <summary>
    /// Получает максимальную длину текста одного документа.
    /// </summary>
    public int MaxDocumentTextLength { get; init; }

    /// <summary>
    /// Получает настройки снимка поискового индекса.
    /// </summary>
    public SearchIndexSnapshotConfigResponse Snapshot { get; init; } = new();
  }

  /// <summary>
  /// Ответ с настройками снимка поискового индекса.
  /// </summary>
  private sealed record SearchIndexSnapshotConfigResponse
  {
    /// <summary>
    /// Получает признак включения сохранения снимка индекса.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Получает признак автоматического восстановления индекса при старте сервиса.
    /// </summary>
    public bool AutoRestoreOnStart { get; init; }

    /// <summary>
    /// Получает путь к файлу снимка индекса.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;
  }
}