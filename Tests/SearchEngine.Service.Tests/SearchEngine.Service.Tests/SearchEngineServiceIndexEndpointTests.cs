using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

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
}