using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

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
  }
}