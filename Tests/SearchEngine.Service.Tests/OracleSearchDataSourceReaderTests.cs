using Microsoft.Extensions.Configuration;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты Oracle-reader-а источника данных.
/// </summary>
/// <remarks>
/// Oracle требует внешнего сервера, поэтому здесь проверяются имя provider-а и общая
/// валидация профиля. Механика SQL-чтения покрыта интеграционными тестами SQLite-reader-а,
/// так как все SQL-reader-ы используют общий базовый класс.
/// </remarks>
public sealed class OracleSearchDataSourceReaderTests
{
  /// <summary>
  /// Проверяет имя provider-а Oracle.
  /// </summary>
  [Fact]
  public void Provider_ВозвращаетИмяOracle()
  {
    // Arrange
    OracleSearchDataSourceReader sut = new(new ConfigurationBuilder().Build());

    // Act
    string provider = sut.Provider;

    // Assert
    Assert.Equal("oracle", provider);
  }

  /// <summary>
  /// Проверяет, что без строки подключения профиль отклоняется общей валидацией.
  /// </summary>
  [Fact]
  public void ValidateProfile_БезСтрокиПодключения_ВозвращаетОшибку()
  {
    // Arrange
    OracleSearchDataSourceReader sut = new(new ConfigurationBuilder().Build());

    SearchDataSourceOptions options = new()
    {
      Query = "select id, text from search_documents"
    };

    // Act
    ApiError? error = sut.ValidateProfile("oracle-demo", options);

    // Assert
    Assert.NotNull(error);
    Assert.Equal("DataSourceConnectionStringNameIsEmpty", error!.Code);
  }

  /// <summary>
  /// Проверяет, что корректный профиль проходит валидацию.
  /// </summary>
  [Fact]
  public void ValidateProfile_СКорректнымПрофилем_ВозвращаетNull()
  {
    // Arrange
    OracleSearchDataSourceReader sut = new(new ConfigurationBuilder().Build());

    SearchDataSourceOptions options = new()
    {
      ConnectionStringName = "ORACLE_DEMO",
      Query = "select id, text from search_documents"
    };

    // Act
    ApiError? error = sut.ValidateProfile("oracle-demo", options);

    // Assert
    Assert.Null(error);
  }
}
