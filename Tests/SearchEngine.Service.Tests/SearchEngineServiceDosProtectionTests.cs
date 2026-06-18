using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Интеграционные тесты защиты сервиса от перегрузки извне.
/// </summary>
public sealed class SearchEngineServiceDosProtectionTests
{
  /// <summary>
  /// Проверяет, что запрос с телом больше лимита отклоняется с кодом 413.
  /// </summary>
  [Fact]
  public async Task PostIndex_СТеломБольшеЛимита_Возвращает413()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactory(
        new Dictionary<string, string?>
        {
          ["SearchEngineService:Limits:MaxRequestBodyBytes"] = "16"
        });

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      isPhoneticSearch = false,
      documents = new[]
      {
        new { id = 1, text = "Текст заметно длиннее шестнадцати байт" }
      }
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("RequestBodyTooLarge", error.Code);
  }

  /// <summary>
  /// Проверяет, что превышение лимита частоты запросов отклоняется с кодом 429.
  /// </summary>
  [Fact]
  public async Task Health_ПриПревышенииЛимитаЧастоты_Возвращает429()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactory(
        new Dictionary<string, string?>
        {
          ["SearchEngineService:Limits:RateLimit:PermitLimit"] = "1",
          ["SearchEngineService:Limits:RateLimit:WindowSeconds"] = "60"
        });

    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage first = await client.GetAsync("/health");
    HttpResponseMessage second = await client.GetAsync("/health");

    // Assert
    Assert.Equal(HttpStatusCode.OK, first.StatusCode);
    Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
  }

  /// <summary>
  /// Проверяет, что при выключенном ограничении частоты лимит не срабатывает.
  /// </summary>
  [Fact]
  public async Task Health_ПриВыключенномОграниченииЧастоты_НеСрабатывает()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactory(
        new Dictionary<string, string?>
        {
          ["SearchEngineService:Limits:RateLimit:IsEnabled"] = "false",
          ["SearchEngineService:Limits:RateLimit:PermitLimit"] = "1"
        });

    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage first = await client.GetAsync("/health");
    HttpResponseMessage second = await client.GetAsync("/health");

    // Assert
    Assert.Equal(HttpStatusCode.OK, first.StatusCode);
    Assert.Equal(HttpStatusCode.OK, second.StatusCode);
  }

  /// <summary>
  /// Создаёт фабрику приложения с переопределёнными настройками.
  /// </summary>
  /// <param name="settings">Переопределяемые настройки конфигурации.</param>
  /// <returns>Фабрика приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> settings)
  {
    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(webHost =>
            webHost.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings)));
  }
}
