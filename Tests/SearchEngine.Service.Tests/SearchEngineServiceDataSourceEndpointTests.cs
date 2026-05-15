using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Интеграционные тесты endpoint-а источников данных.
/// </summary>
public sealed class SearchEngineServiceDataSourceEndpointTests
{
  /// <summary>
  /// Проверяет, что без настроенных источников endpoint возвращает пустой список.
  /// </summary>
  [Fact]
  public async Task GetDataSources_БезИсточников_ВозвращаетПустойСписок()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    // Act
    SearchDataSourcesResponse? response =
        await client.GetFromJsonAsync<SearchDataSourcesResponse>("/v1/data-sources");

    // Assert
    Assert.NotNull(response);
    Assert.Empty(response.Items);
    Assert.Empty(response.SupportedProviders);
  }

  /// <summary>
  /// Проверяет, что endpoint возвращает безопасное описание настроенных источников.
  /// </summary>
  [Fact]
  public async Task GetDataSources_СПрофилямиИсточников_ВозвращаетБезопасноеОписание()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = CreateFactoryWithDataSources();

    using HttpClient client = factory.CreateClient();

    // Act
    SearchDataSourcesResponse? response =
        await client.GetFromJsonAsync<SearchDataSourcesResponse>("/v1/data-sources");

    // Assert
    Assert.NotNull(response);

    SearchDataSourceResponse source = Assert.Single(response.Items);

    Assert.Equal("products", source.Name);
    Assert.True(source.IsEnabled);
    Assert.Equal("postgres", source.Provider);
    Assert.True(source.HasConnectionStringName);
    Assert.True(source.HasQuery);
    Assert.Empty(response.SupportedProviders);
    Assert.False(source.IsProviderSupported);
  }

  /// <summary>
  /// Создаёт фабрику приложения с тестовыми источниками данных.
  /// </summary>
  /// <returns>Фабрика тестового приложения.</returns>
  private static WebApplicationFactory<Program> CreateFactoryWithDataSources()
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
                      ["SearchEngineService:Sources:products:ConnectionStringName"] = "PRODUCTS_DB",
                      ["SearchEngineService:Sources:products:Query"] = "select id, name as text from products"
                    });
          });
        });
  }
}