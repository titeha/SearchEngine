using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты registry provider-ов источников данных.
/// </summary>
public sealed class SearchDataSourceReaderRegistryTests
{
  /// <summary>
  /// Проверяет, что пустой registry не содержит поддерживаемых provider-ов.
  /// </summary>
  [Fact]
  public void GetSupportedProviders_БезReader_ВозвращаетПустойСписок()
  {
    // Arrange
    SearchDataSourceReaderRegistry sut = new([]);

    // Act
    string[] result = sut.GetSupportedProviders();

    // Assert
    Assert.Empty(result);
    Assert.False(sut.IsSupported("postgres"));
    Assert.Null(sut.GetReader("postgres"));
  }

  /// <summary>
  /// Проверяет, что registry находит reader по имени provider-а без учёта регистра.
  /// </summary>
  [Fact]
  public void GetReader_ПриЗарегистрированномReader_ВозвращаетReaderБезУчетаРегистра()
  {
    // Arrange
    TestSearchDataSourceReader reader = new("postgres");

    SearchDataSourceReaderRegistry sut = new(
    [
        reader
    ]);

    // Act
    ISearchDataSourceReader? result = sut.GetReader("POSTGRES");

    // Assert
    Assert.True(sut.IsSupported("postgres"));
    Assert.True(sut.IsSupported("POSTGRES"));
    Assert.Single(sut.GetSupportedProviders());
    Assert.Same(reader, result);
  }

  /// <summary>
  /// Проверяет, что registry убирает пробелы вокруг имени provider-а.
  /// </summary>
  [Fact]
  public void GetReader_ПриПробелахВProvider_ВозвращаетReader()
  {
    // Arrange
    TestSearchDataSourceReader reader = new("postgres");

    SearchDataSourceReaderRegistry sut = new(
    [
        reader
    ]);

    // Act
    ISearchDataSourceReader? result = sut.GetReader(" postgres ");

    // Assert
    Assert.True(sut.IsSupported(" postgres "));
    Assert.Same(reader, result);
  }

  /// <summary>
  /// Проверяет, что registry хранит нормализованное имя provider-а.
  /// </summary>
  [Fact]
  public void GetSupportedProviders_ПриПробелахВProviderReader_ВозвращаетНормализованноеИмя()
  {
    // Arrange
    SearchDataSourceReaderRegistry sut = new(
    [
        new TestSearchDataSourceReader(" postgres ")
    ]);

    // Act
    string[] result = sut.GetSupportedProviders();

    // Assert
    Assert.Equal(["postgres"], result);
  }

  /// <summary>
  /// Тестовый reader источника данных.
  /// </summary>
  /// <param name="provider">Имя provider-а.</param>
  private sealed class TestSearchDataSourceReader(string provider) : ISearchDataSourceReader
  {
    /// <inheritdoc />
    public string Provider { get; } = provider;

    /// <inheritdoc />
    public ApiError? ValidateProfile(
        string sourceName,
        SearchDataSourceOptions options)
    {
      return null;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchDataSourceDocument>> ReadAsync(
        string sourceName,
        SearchDataSourceOptions options,
        CancellationToken cancellationToken = default)
    {
      IReadOnlyList<SearchDataSourceDocument> result =
      [
          new()
                {
                    Id = 1,
                    Text = "Тестовый документ"
                }
      ];

      return Task.FromResult(result);
    }
  }
}
