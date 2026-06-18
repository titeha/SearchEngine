using Microsoft.Extensions.Configuration;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты Firebird-reader-а источника данных.
/// </summary>
/// <remarks>
/// Firebird требует внешнего сервера, поэтому здесь проверяются имя provider-а и общая
/// валидация профиля. Механика SQL-чтения покрыта интеграционными тестами SQLite-reader-а,
/// так как все SQL-reader-ы используют общий базовый класс.
/// </remarks>
public sealed class FirebirdSearchDataSourceReaderTests
{
  /// <summary>
  /// Проверяет имя provider-а Firebird.
  /// </summary>
  [Fact]
  public void Provider_ВозвращаетИмяFirebird()
  {
    // Arrange
    FirebirdSearchDataSourceReader sut = new(new ConfigurationBuilder().Build());

    // Act
    string provider = sut.Provider;

    // Assert
    Assert.Equal("firebird", provider);
  }

  /// <summary>
  /// Проверяет, что без строки подключения профиль отклоняется общей валидацией.
  /// </summary>
  [Fact]
  public void ValidateProfile_БезСтрокиПодключения_ВозвращаетОшибку()
  {
    // Arrange
    FirebirdSearchDataSourceReader sut = new(new ConfigurationBuilder().Build());

    SearchDataSourceOptions options = new()
    {
      Query = "select id, text from search_documents"
    };

    // Act
    ApiError? error = sut.ValidateProfile("firebird-demo", options);

    // Assert
    Assert.NotNull(error);
    Assert.Equal("DataSourceConnectionStringNameIsEmpty", error!.Code);
  }

  /// <summary>
  /// Проверяет, что без SQL-запроса профиль отклоняется общей валидацией.
  /// </summary>
  [Fact]
  public void ValidateProfile_БезЗапроса_ВозвращаетОшибку()
  {
    // Arrange
    FirebirdSearchDataSourceReader sut = new(new ConfigurationBuilder().Build());

    SearchDataSourceOptions options = new()
    {
      ConnectionStringName = "FIREBIRD_DEMO"
    };

    // Act
    ApiError? error = sut.ValidateProfile("firebird-demo", options);

    // Assert
    Assert.NotNull(error);
    Assert.Equal("DataSourceQueryIsEmpty", error!.Code);
  }

  /// <summary>
  /// Проверяет, что корректный профиль проходит валидацию.
  /// </summary>
  [Fact]
  public void ValidateProfile_СКорректнымПрофилем_ВозвращаетNull()
  {
    // Arrange
    FirebirdSearchDataSourceReader sut = new(new ConfigurationBuilder().Build());

    SearchDataSourceOptions options = new()
    {
      ConnectionStringName = "FIREBIRD_DEMO",
      Query = "select id, text from search_documents"
    };

    // Act
    ApiError? error = sut.ValidateProfile("firebird-demo", options);

    // Assert
    Assert.Null(error);
  }
}
