using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Интеграционные тесты HTTP-endpoint-а поиска.
/// </summary>
public sealed class SearchEngineServiceSearchEndpointTests
{
  /// <summary>
  /// Проверяет, что поиск до построения индекса возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task PostSearch_ДоПостроенияИндекса_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      query = "Иванов"
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/search", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    Assert.NotNull(error);
    Assert.Equal("IndexNotBuilt", error.Code);
  }

  /// <summary>
  /// Проверяет, что пустая поисковая строка возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task PostSearch_СПустымЗапросом_ВозвращаетBadRequest()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    object request = new
    {
      query = "   "
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/search", request);

    ApiError? error = await response.Content.ReadFromJsonAsync<ApiError>();

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    Assert.NotNull(error);
    Assert.Equal("EmptyQuery", error.Code);
  }

  /// <summary>
  /// Проверяет, что точный поиск по началу слова возвращает найденный документ.
  /// </summary>
  [Fact]
  public async Task PostSearch_ТочныйПоискПоНачалуСлова_ВозвращаетДокумент()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    await BuildIndexAsync(client);

    object request = new
    {
      query = "Иванов",
      matchMode = "AllTerms",
      searchType = "ExactSearch",
      searchLocation = "BeginWord"
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/search", request);

    SearchQueryResponse? searchResult =
        await response.Content.ReadFromJsonAsync<SearchQueryResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.NotNull(searchResult);
    Assert.True(searchResult.IsHasIndex);
    Assert.True(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
    Assert.False(ContainsId(searchResult, 3));
  }

  /// <summary>
  /// Проверяет, что точный поиск внутри слова возвращает найденный документ.
  /// </summary>
  [Fact]
  public async Task PostSearch_ТочныйПоискВнутриСлова_ВозвращаетДокумент()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    await BuildIndexAsync(client);

    object request = new
    {
      query = "лосип",
      matchMode = "AllTerms",
      searchType = "ExactSearch",
      searchLocation = "InWord"
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/search", request);

    SearchQueryResponse? searchResult =
        await response.Content.ReadFromJsonAsync<SearchQueryResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.NotNull(searchResult);
    Assert.True(searchResult.IsHasIndex);
    Assert.False(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
    Assert.True(ContainsId(searchResult, 3));
  }

  /// <summary>
  /// Проверяет, что нечёткий поиск через допустимое количество опечаток возвращает найденный документ.
  /// </summary>
  /// <param name="query">Поисковая строка с опечаткой.</param>
  /// <param name="acceptableCountMisprint">Допустимая взвешенная дистанция.</param>
  [Theory]
  [InlineData("Иваноы", 1)]
  [InlineData("Иваноф", 2)]
  public async Task PostSearch_НечеткийПоискЧерезКоличествоОпечаток_ВозвращаетДокумент(
      string query,
      int acceptableCountMisprint)
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    await BuildIndexAsync(client);

    object request = new
    {
      query,
      matchMode = "AllTerms",
      searchType = "NearSearch",
      searchLocation = "BeginWord",
      acceptableCountMisprint
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/search", request);

    SearchQueryResponse? searchResult =
        await response.Content.ReadFromJsonAsync<SearchQueryResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.NotNull(searchResult);
    Assert.True(searchResult.IsHasIndex);
    Assert.True(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
    Assert.False(ContainsId(searchResult, 3));
  }

  /// <summary>
  /// Проверяет, что нечёткий поиск через процент точности возвращает найденный документ.
  /// </summary>
  [Fact]
  public async Task PostSearch_НечеткийПоискЧерезПроцентТочности_ВозвращаетДокумент()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    await BuildIndexAsync(client);

    object request = new
    {
      query = "веласипед",
      matchMode = "AllTerms",
      searchType = "NearSearch",
      searchLocation = "BeginWord",
      precisionSearch = 70
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/search", request);

    SearchQueryResponse? searchResult =
        await response.Content.ReadFromJsonAsync<SearchQueryResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.NotNull(searchResult);
    Assert.True(searchResult.IsHasIndex);
    Assert.False(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
    Assert.True(ContainsId(searchResult, 3));
  }

  /// <summary>
  /// Проверяет, что без фонетического индекса латинская запись не находит кириллическую фамилию через HTTP API.
  /// </summary>
  [Fact]
  public async Task PostSearch_БезФонетическогоИндекса_НеИщетРусскуюФамилиюВЛатинскойЗаписи()
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    await BuildIndexAsync(client);

    object request = new
    {
      query = "Ivanov",
      matchMode = "AllTerms",
      searchType = "ExactSearch",
      searchLocation = "BeginWord"
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/search", request);

    SearchQueryResponse? searchResult = await response.Content.ReadFromJsonAsync<SearchQueryResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.NotNull(searchResult);
    Assert.False(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
    Assert.False(ContainsId(searchResult, 3));
  }

  /// <summary>
  /// Проверяет, что фонетический индекс ищет русские фамилии в латинской записи через HTTP API.
  /// </summary>
  /// <param name="query">Поисковая строка в латинской записи.</param>
  /// <param name="expectedId">Ожидаемый идентификатор документа.</param>
  [Theory]
  [InlineData("Ivanov", 1)]
  [InlineData("Papandopulo", 2)]
  [InlineData("Papondopulo", 2)]
  public async Task PostSearch_СФонетическимИндексом_ИщетРусскиеФамилииВЛатинскойЗаписи(
      string query,
      int expectedId)
  {
    // Arrange
    await using WebApplicationFactory<Program> factory = new();

    using HttpClient client = factory.CreateClient();

    await BuildIndexAsync(client, isPhoneticSearch: true);

    object request = new
    {
      query,
      matchMode = "AllTerms",
      searchType = "ExactSearch",
      searchLocation = "BeginWord"
    };

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/search", request);

    SearchQueryResponse? searchResult =
        await response.Content.ReadFromJsonAsync<SearchQueryResponse>();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.NotNull(searchResult);
    Assert.True(searchResult.IsHasIndex);
    Assert.True(ContainsId(searchResult, expectedId));
  }

  /// <summary>
  /// Строит тестовый индекс через HTTP API сервиса.
  /// </summary>
  /// <param name="client">HTTP-клиент тестового сервера.</param>
  /// <param name="isPhoneticSearch">Признак включения фонетического поиска.</param>
  private static async Task BuildIndexAsync(
      HttpClient client,
      bool isPhoneticSearch = false)
  {
    IndexBuildRequest request = new()
    {
      IsPhoneticSearch = isPhoneticSearch,
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

    HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", request);

    response.EnsureSuccessStatusCode();
  }

  /// <summary>
  /// Проверяет наличие идентификатора документа в ответе поиска.
  /// </summary>
  /// <param name="response">Ответ поиска.</param>
  /// <param name="id">Идентификатор документа.</param>
  /// <returns><see langword="true"/>, если идентификатор найден.</returns>
  private static bool ContainsId(SearchQueryResponse response, int id) => response.Items.Any(bucket => bucket.Ids.Contains(id));
}