using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Интеграционные тесты HTTP-endpoint-ов в режиме нескольких именованных индексов.
/// </summary>
public sealed class SearchEngineServiceMultiIndexEndpointTests
{
  /// <summary>
  /// Проверяет, что построение именованного индекса не делает готовым индекс по умолчанию,
  /// а список индексов и поиск учитывают имя индекса.
  /// </summary>
  [Fact]
  public async Task PostIndex_СИменемИндекса_СтроитТолькоЭтотИндекс()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();
    using HttpClient client = factory.CreateClient();

    object buildRequest = new
    {
      index = "products",
      isPhoneticSearch = false,
      documents = new[]
      {
        new { id = 1, text = "Красный велосипед" }
      }
    };

    // Act
    HttpResponseMessage buildResponse = await client.PostAsJsonAsync("/v1/index", buildRequest);

    IndexStatusResponse? productsStatus =
        await client.GetFromJsonAsync<IndexStatusResponse>("/v1/index?index=products");

    IndexStatusResponse? defaultStatus =
        await client.GetFromJsonAsync<IndexStatusResponse>("/v1/index");

    SearchIndexListResponse? indexes =
        await client.GetFromJsonAsync<SearchIndexListResponse>("/v1/indexes");

    HttpResponseMessage searchResponse = await client.PostAsJsonAsync(
        "/v1/search",
        new { index = "products", query = "велосипед" });

    SearchQueryResponse? searchResult =
        await searchResponse.Content.ReadFromJsonAsync<SearchQueryResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.OK, buildResponse.StatusCode);

    Assert.NotNull(productsStatus);
    Assert.Equal(IndexState.Ready, productsStatus.State);
    Assert.Equal("products", productsStatus.IndexName);

    Assert.NotNull(defaultStatus);
    Assert.Equal(IndexState.NotBuilt, defaultStatus.State);

    Assert.NotNull(indexes);
    Assert.Contains(indexes.Items, item => item.IndexName == "products");

    Assert.NotNull(searchResult);
    Assert.Contains(searchResult.Items, bucket => bucket.Ids.Contains(1));
  }

  /// <summary>
  /// Проверяет, что недопустимое имя индекса возвращает BadRequest.
  /// </summary>
  [Fact]
  public async Task PostIndex_СНедопустимымИменемИндекса_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();
    using HttpClient client = factory.CreateClient();

    object buildRequest = new
    {
      index = "../escape",
      documents = new[]
      {
        new { id = 1, text = "Красный велосипед" }
      }
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", buildRequest);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.NotNull(error);
    Assert.Equal("InvalidIndexName", error.Code);
  }
}
