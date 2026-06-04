using Microsoft.Extensions.Options;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты построения индекса из источника данных.
/// </summary>
public sealed class SearchIndexFromSourceBuilderTests
{
  /// <summary>
  /// Проверяет, что пустое имя источника данных возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task BuildAsync_СПустымИменемИсточника_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexFromSourceBuilder sut = CreateBuilder();

    IndexBuildFromSourceRequest request = new()
    {
      SourceName = " ",
      IsPhoneticSearch = true
    };

    // Act
    (IndexStatusResponse? status, ApiError? error) = await sut.BuildAsync(request);

    // Assert
    Assert.Null(status);
    Assert.NotNull(error);
    Assert.Equal("EmptySourceName", error.Code);
  }

  /// <summary>
  /// Проверяет, что неизвестный источник данных возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task BuildAsync_СНеизвестнымИсточником_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexFromSourceBuilder sut = CreateBuilder();

    IndexBuildFromSourceRequest request = new()
    {
      SourceName = "products",
      IsPhoneticSearch = true
    };

    // Act
    (IndexStatusResponse? status, ApiError? error) = await sut.BuildAsync(request);

    // Assert
    Assert.Null(status);
    Assert.NotNull(error);
    Assert.Equal("DataSourceNotFound", error.Code);
  }

  /// <summary>
  /// Проверяет, что отключённый источник данных возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task BuildAsync_СОтключеннымИсточником_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexFromSourceBuilder sut = CreateBuilder(
        sources: new Dictionary<string, SearchDataSourceOptions>
        {
          ["products"] = new()
          {
            IsEnabled = false,
            Provider = InMemorySearchDataSourceReader.ProviderName
          }
        },
        readers:
        [
            new InMemorySearchDataSourceReader()
        ]);

    IndexBuildFromSourceRequest request = new()
    {
      SourceName = "products",
      IsPhoneticSearch = true
    };

    // Act
    (IndexStatusResponse? status, ApiError? error) = await sut.BuildAsync(request);

    // Assert
    Assert.Null(status);
    Assert.NotNull(error);
    Assert.Equal("DataSourceDisabled", error.Code);
  }

  /// <summary>
  /// Проверяет, что неподдерживаемый provider возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public async Task BuildAsync_СНеподдерживаемымProvider_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexFromSourceBuilder sut = CreateBuilder(
        sources: new Dictionary<string, SearchDataSourceOptions>
        {
          ["products"] = new()
          {
            IsEnabled = true,
            Provider = "oracle"
          }
        });

    IndexBuildFromSourceRequest request = new()
    {
      SourceName = "products",
      IsPhoneticSearch = true
    };

    // Act
    (IndexStatusResponse? status, ApiError? error) = await sut.BuildAsync(request);

    // Assert
    Assert.Null(status);
    Assert.NotNull(error);
    Assert.Equal("DataSourceProviderNotSupported", error.Code);
  }

  /// <summary>
  /// Проверяет, что ошибка reader-а возвращается как прикладная ошибка.
  /// </summary>
  [Fact]
  public async Task BuildAsync_ПриОшибкеReader_ВозвращаетОшибку()
  {
    // Arrange
    SearchIndexFromSourceBuilder sut = CreateBuilder(
        sources: new Dictionary<string, SearchDataSourceOptions>
        {
          ["broken"] = new()
          {
            IsEnabled = true,
            Provider = "broken"
          }
        },
        readers:
        [
            new BrokenSearchDataSourceReader()
        ]);

    IndexBuildFromSourceRequest request = new()
    {
      SourceName = "broken",
      IsPhoneticSearch = true
    };

    // Act
    (IndexStatusResponse? status, ApiError? error) = await sut.BuildAsync(request);

    // Assert
    Assert.Null(status);
    Assert.NotNull(error);
    Assert.Equal("DataSourceReadFailed", error.Code);
  }

  /// <summary>
  /// Проверяет, что индекс строится из in-memory источника данных.
  /// </summary>
  [Fact]
  public async Task BuildAsync_СInMemoryProvider_СтроитИндекс()
  {
    // Arrange
    SearchIndexStore store = new();

    SearchIndexFromSourceBuilder sut = CreateBuilder(
        store,
        sources: new Dictionary<string, SearchDataSourceOptions>
        {
          ["demo"] = new()
          {
            IsEnabled = true,
            Provider = InMemorySearchDataSourceReader.ProviderName,
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
                        }
                ]
          }
        },
        readers:
        [
            new InMemorySearchDataSourceReader()
        ]);

    IndexBuildFromSourceRequest request = new()
    {
      SourceName = "demo",
      IsPhoneticSearch = true
    };

    // Act
    (IndexStatusResponse? status, ApiError? error) = await sut.BuildAsync(request);

    SearchQueryResponse? searchResult = store.Search(
        new SearchQueryRequest
        {
          Query = "Ivanov"
        },
        out ApiError? searchError);

    // Assert
    Assert.Null(error);

    Assert.NotNull(status);
    Assert.True(status.IsReady);
    Assert.Equal(2, status.DocumentCount);
    Assert.Equal(2, status.SearchableDocumentCount);
    Assert.True(status.IsPhoneticSearch);

    Assert.Null(searchError);
    Assert.NotNull(searchResult);
    Assert.True(ContainsId(searchResult, 1));
    Assert.False(ContainsId(searchResult, 2));
  }

  /// <summary>
  /// Проверяет, что имя источника данных ищется без учёта регистра.
  /// </summary>
  [Fact]
  public async Task BuildAsync_СИменемИсточникаВДругомРегистре_СтроитИндекс()
  {
    // Arrange
    SearchIndexFromSourceBuilder sut = CreateBuilder(
        sources: new Dictionary<string, SearchDataSourceOptions>
        {
          ["Demo"] = new()
          {
            IsEnabled = true,
            Provider = InMemorySearchDataSourceReader.ProviderName,
            Documents =
                [
                    new()
                        {
                            Id = 1,
                            Text = "Иванов Сергей Петрович"
                        }
                ]
          }
        },
        readers:
        [
            new InMemorySearchDataSourceReader()
        ]);

    IndexBuildFromSourceRequest request = new()
    {
      SourceName = "demo",
      IsPhoneticSearch = false
    };

    // Act
    (IndexStatusResponse? status, ApiError? error) = await sut.BuildAsync(request);

    // Assert
    Assert.Null(error);

    Assert.NotNull(status);
    Assert.True(status.IsReady);
    Assert.Equal(1, status.DocumentCount);
    Assert.Equal(1, status.SearchableDocumentCount);
  }

  /// <summary>
  /// Создаёт builder с настройками по умолчанию.
  /// </summary>
  /// <param name="sources">Источники данных.</param>
  /// <param name="readers">Reader-ы источников данных.</param>
  /// <returns>Builder построения индекса из источника данных.</returns>
  private static SearchIndexFromSourceBuilder CreateBuilder(
      Dictionary<string, SearchDataSourceOptions>? sources = null,
      IReadOnlyList<ISearchDataSourceReader>? readers = null)
  {
    return CreateBuilder(new SearchIndexStore(), sources, readers);
  }

  /// <summary>
  /// Создаёт builder с указанным хранилищем индекса.
  /// </summary>
  /// <param name="store">Хранилище поискового индекса.</param>
  /// <param name="sources">Источники данных.</param>
  /// <param name="readers">Reader-ы источников данных.</param>
  /// <returns>Builder построения индекса из источника данных.</returns>
  private static SearchIndexFromSourceBuilder CreateBuilder(
      SearchIndexStore store,
      Dictionary<string, SearchDataSourceOptions>? sources = null,
      IReadOnlyList<ISearchDataSourceReader>? readers = null)
  {
    SearchEngineServiceOptions options = new()
    {
      Sources = sources ?? []
    };

    SearchDataSourceReaderRegistry registry = new(readers ?? []);

    return new SearchIndexFromSourceBuilder(
        Options.Create(options),
        registry,
        new SearchDataSourceProfileValidator(),
        store);
  }

  /// <summary>
  /// Проверяет наличие идентификатора документа в ответе поиска.
  /// </summary>
  /// <param name="response">Ответ поиска.</param>
  /// <param name="id">Идентификатор документа.</param>
  /// <returns><see langword="true"/>, если идентификатор найден.</returns>
  private static bool ContainsId(SearchQueryResponse response, int id)
  {
    return response.Items.Any(bucket => bucket.Ids.Contains(id));
  }

  /// <summary>
  /// Reader источника данных, который всегда возвращает ошибку.
  /// </summary>
  private sealed class BrokenSearchDataSourceReader : ISearchDataSourceReader
  {
    /// <inheritdoc />
    public string Provider => "broken";

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchDataSourceDocument>> ReadAsync(
        string sourceName,
        SearchDataSourceOptions options,
        CancellationToken cancellationToken = default)
    {
      throw new InvalidOperationException("Тестовая ошибка чтения источника данных.");
    }
  }
}