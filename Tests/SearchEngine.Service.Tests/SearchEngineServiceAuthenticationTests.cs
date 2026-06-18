using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Интеграционные тесты аутентификации по API-ключу.
/// </summary>
public sealed class SearchEngineServiceAuthenticationTests
{
  private const string _apiKey = "test-secret-key";

  /// <summary>
  /// Проверяет, что без заданного ключа мутирующий endpoint открыт (рабочий внутренний сценарий).
  /// </summary>
  [Fact]
  public async Task PostIndex_БезЗаданногоКлюча_Открыт()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactory(apiKey: null);
    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", CreateBuildRequest());

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  /// <summary>
  /// Проверяет, что при заданном ключе мутирующий запрос без ключа отклоняется с кодом 401.
  /// </summary>
  [Fact]
  public async Task PostIndex_СЗаданнымКлючомБезЗаголовка_Возвращает401()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactory(apiKey: _apiKey);
    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", CreateBuildRequest());

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("Unauthorized", error.Code);
  }

  /// <summary>
  /// Проверяет, что мутирующий запрос с неверным ключом отклоняется с кодом 401.
  /// </summary>
  [Fact]
  public async Task PostIndex_СНевернымКлючом_Возвращает401()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactory(apiKey: _apiKey);
    using HttpClient client = factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", CreateBuildRequest());

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  /// <summary>
  /// Проверяет, что мутирующий запрос с верным ключом выполняется.
  /// </summary>
  [Fact]
  public async Task PostIndex_СВернымКлючом_СтроитИндекс()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactory(apiKey: _apiKey);
    using HttpClient client = factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", CreateBuildRequest());

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  /// <summary>
  /// Проверяет, что при заданном ключе чтение состояния остаётся открытым.
  /// </summary>
  [Fact]
  public async Task GetIndex_СЗаданнымКлючом_Открыт()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactory(apiKey: _apiKey);
    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.GetAsync("/v1/index");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  /// <summary>
  /// Проверяет, что при заданном ключе поиск остаётся открытым (не требует ключа).
  /// </summary>
  [Fact]
  public async Task PostSearch_СЗаданнымКлючомБезЗаголовка_НеТребуетКлюч()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactory(apiKey: _apiKey);
    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/search", new { query = "Иванов" });

    // Assert
    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  /// <summary>
  /// Создаёт запрос на построение индекса с одним документом.
  /// </summary>
  /// <returns>Запрос на построение индекса.</returns>
  private static object CreateBuildRequest()
  {
    return new
    {
      isPhoneticSearch = false,
      documents = new[]
      {
        new { id = 1, text = "Иванов Сергей Петрович" }
      }
    };
  }

  /// <summary>
  /// Создаёт фабрику приложения с заданным API-ключом.
  /// </summary>
  /// <param name="apiKey">API-ключ или <see langword="null"/>, если ключ не задан.</param>
  /// <returns>Фабрика приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactory(string? apiKey)
  {
    Dictionary<string, string?> settings = new()
    {
      ["SearchEngineService:Authentication:ApiKey"] = apiKey
    };

    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(webHost =>
            webHost.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings)));
  }
}
