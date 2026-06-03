using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты проверки профилей источников данных.
/// </summary>
public sealed class SearchDataSourceProfileValidatorTests
{
  /// <summary>
  /// Проверяет, что профиль без provider-а возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public void Validate_БезProvider_ВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = new();

    SearchDataSourceOptions source = new()
    {
      Provider = " "
    };

    // Act
    ApiError? result = sut.Validate("products", source);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("DataSourceProviderIsEmpty", result.Code);
  }

  /// <summary>
  /// Проверяет, что SQLite-профиль без имени строки подключения возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public void Validate_SqliteБезConnectionStringName_ВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = new();

    SearchDataSourceOptions source = new()
    {
      Provider = "sqlite",
      ConnectionStringName = " ",
      Query = "select id, text from search_documents"
    };

    // Act
    ApiError? result = sut.Validate("sqlite-demo", source);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("DataSourceConnectionStringNameIsEmpty", result.Code);
  }

  /// <summary>
  /// Проверяет, что SQLite-профиль без SQL-запроса возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public void Validate_SqliteБезQuery_ВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = new();

    SearchDataSourceOptions source = new()
    {
      Provider = "sqlite",
      ConnectionStringName = "SQLITE_DEMO",
      Query = " "
    };

    // Act
    ApiError? result = sut.Validate("sqlite-demo", source);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("DataSourceQueryIsEmpty", result.Code);
  }

  /// <summary>
  /// Проверяет, что корректный SQLite-профиль проходит проверку.
  /// </summary>
  [Fact]
  public void Validate_КорректныйSqliteПрофиль_НеВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = new();

    SearchDataSourceOptions source = new()
    {
      Provider = "sqlite",
      ConnectionStringName = "SQLITE_DEMO",
      Query = "select id, text from search_documents"
    };

    // Act
    ApiError? result = sut.Validate("sqlite-demo", source);

    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Проверяет, что in-memory provider не требует строки подключения и SQL-запроса.
  /// </summary>
  [Fact]
  public void Validate_InMemoryПрофиль_НеТребуетConnectionStringNameИQuery()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = new();

    SearchDataSourceOptions source = new()
    {
      Provider = "in-memory"
    };

    // Act
    ApiError? result = sut.Validate("demo", source);

    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Проверяет, что неизвестный provider проходит базовую проверку.
  /// </summary>
  [Fact]
  public void Validate_НеизвестныйProvider_НеВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = new();

    SearchDataSourceOptions source = new()
    {
      Provider = "postgres"
    };

    // Act
    ApiError? result = sut.Validate("products", source);

    // Assert
    Assert.Null(result);
  }
}