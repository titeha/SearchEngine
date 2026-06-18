using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace SearchEngineService.Tests;

/// <summary>
/// Интеграционные тесты OpenAPI-описания сервиса.
/// </summary>
public sealed class SearchEngineServiceOpenApiTests
{
  /// <summary>
  /// Проверяет, что OpenAPI-документ доступен и описывает ключевые endpoint-ы.
  /// </summary>
  [Fact]
  public async Task GetOpenApiDocument_ВозвращаетОписаниеКлючевыхEndpoint()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();
    using HttpClient client = factory.CreateClient();

    // Act
    HttpResponseMessage response = await client.GetAsync("/openapi/v1.json");

    string document = await response.Content.ReadAsStringAsync();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("/v1/search", document);
    Assert.Contains("/v1/index", document);
    Assert.Contains("/v1/indexes", document);
  }
}
