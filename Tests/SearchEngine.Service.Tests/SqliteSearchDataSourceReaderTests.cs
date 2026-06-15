using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты SQLite-reader-а источника данных.
/// </summary>
public sealed class SqliteSearchDataSourceReaderTests
{
  /// <summary>
  /// Проверяет, что reader читает документы из SQLite через строку подключения из раздела ConnectionStrings.
  /// </summary>
  [Fact]
  public async Task ReadAsync_СоСтрокойПодключенияИзConnectionStrings_ЧитаетДокументы()
  {
    // Arrange
    string databasePath = CreateDatabasePath();

    try
    {
      string connectionString = CreateConnectionString(databasePath);

      await CreateDemoDatabaseAsync(connectionString);

      IConfiguration configuration = CreateConfiguration(
          "ConnectionStrings:SQLITE_DEMO",
          connectionString);

      SqliteSearchDataSourceReader sut = new(configuration);

      SearchDataSourceOptions options = new()
      {
        ConnectionStringName = "SQLITE_DEMO",
        Query = "select id, text from search_documents order by id"
      };

      // Act
      IReadOnlyList<SearchDataSourceDocument> result = await sut.ReadAsync("sqlite-demo", options);

      // Assert
      Assert.Equal(2, result.Count);

      Assert.Equal(1, result[0].Id);
      Assert.Equal("Иванов Сергей Петрович", result[0].Text);

      Assert.Equal(2, result[1].Id);
      Assert.Equal("Папандопуло Александр", result[1].Text);
    }
    finally
    {
      DeleteDatabase(databasePath);
    }
  }

  /// <summary>
  /// Проверяет, что reader читает строку подключения из обычного ключа конфигурации.
  /// </summary>
  [Fact]
  public async Task ReadAsync_СоСтрокойПодключенияИзКлючаКонфигурации_ЧитаетДокументы()
  {
    // Arrange
    string databasePath = CreateDatabasePath();

    try
    {
      string connectionString = CreateConnectionString(databasePath);

      await CreateDemoDatabaseAsync(connectionString);

      IConfiguration configuration = CreateConfiguration(
          "SQLITE_DIRECT",
          connectionString);

      SqliteSearchDataSourceReader sut = new(configuration);

      SearchDataSourceOptions options = new()
      {
        ConnectionStringName = " SQLITE_DIRECT ",
        Query = "select id, text from search_documents where id = 2"
      };

      // Act
      IReadOnlyList<SearchDataSourceDocument> result = await sut.ReadAsync("sqlite-demo", options);

      // Assert
      SearchDataSourceDocument document = Assert.Single(result);

      Assert.Equal(2, document.Id);
      Assert.Equal("Папандопуло Александр", document.Text);
    }
    finally
    {
      DeleteDatabase(databasePath);
    }
  }

  /// <summary>
  /// Проверяет, что отсутствующая строка подключения возвращается как ошибка чтения профиля.
  /// </summary>
  [Fact]
  public async Task ReadAsync_БезСтрокиПодключения_ВыбрасываетИсключение()
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder().Build();

    SqliteSearchDataSourceReader sut = new(configuration);

    SearchDataSourceOptions options = new()
    {
      ConnectionStringName = "MISSING_CONNECTION_STRING",
      Query = "select 1 as id, 'Тестовый документ' as text"
    };

    // Act
    InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
        () => sut.ReadAsync("sqlite-demo", options));

    // Assert
    Assert.Contains("Строка подключения не найдена", exception.Message);
    Assert.Contains("MISSING_CONNECTION_STRING", exception.Message);
  }

  /// <summary>
  /// Проверяет, что отсутствие обязательной колонки результата возвращает понятную ошибку.
  /// </summary>
  [Fact]
  public async Task ReadAsync_БезОбязательнойКолонкиText_ВыбрасываетПонятноеИсключение()
  {
    // Arrange
    string databasePath = CreateDatabasePath();

    try
    {
      string connectionString = CreateConnectionString(databasePath);

      await CreateDemoDatabaseAsync(connectionString);

      IConfiguration configuration = CreateConfiguration(
          "ConnectionStrings:SQLITE_DEMO",
          connectionString);

      SqliteSearchDataSourceReader sut = new(configuration);

      SearchDataSourceOptions options = new()
      {
        ConnectionStringName = "SQLITE_DEMO",
        Query = "select id from search_documents order by id"
      };

      // Act
      InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
          () => sut.ReadAsync("sqlite-demo", options));

      // Assert
      Assert.Contains("text", exception.Message, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("sqlite-demo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      DeleteDatabase(databasePath);
    }
  }

  /// <summary>
  /// Создаёт путь к временной SQLite-БД.
  /// </summary>
  /// <returns>Путь к временной SQLite-БД.</returns>
  private static string CreateDatabasePath()
  {
    return Path.Combine(
        Path.GetTempPath(),
        $"search-engine-service-{Guid.NewGuid():N}.db");
  }

  /// <summary>
  /// Создаёт строку подключения SQLite к указанному файлу БД.
  /// </summary>
  /// <param name="databasePath">Путь к файлу БД.</param>
  /// <returns>Строка подключения SQLite.</returns>
  private static string CreateConnectionString(string databasePath)
  {
    SqliteConnectionStringBuilder builder = new()
    {
      DataSource = databasePath
    };

    return builder.ToString();
  }

  /// <summary>
  /// Создаёт конфигурацию с одной строкой подключения.
  /// </summary>
  /// <param name="key">Ключ конфигурации.</param>
  /// <param name="connectionString">Строка подключения.</param>
  /// <returns>Конфигурация для теста.</returns>
  private static IConfiguration CreateConfiguration(
      string key,
      string connectionString)
  {
    Dictionary<string, string?> values = new()
    {
      [key] = connectionString
    };

    return new ConfigurationBuilder()
        .AddInMemoryCollection(values)
        .Build();
  }

  /// <summary>
  /// Создаёт demo-таблицу с документами для поиска.
  /// </summary>
  /// <param name="connectionString">Строка подключения SQLite.</param>
  private static async Task CreateDemoDatabaseAsync(string connectionString)
  {
    await using SqliteConnection connection = new(connectionString);

    await connection.OpenAsync();

    await using SqliteCommand command = connection.CreateCommand();

    command.CommandText = """
        create table search_documents
        (
            id integer not null primary key,
            text text not null
        );

        insert into search_documents(id, text)
        values
            (1, 'Иванов Сергей Петрович'),
            (2, 'Папандопуло Александр');
        """;

    await command.ExecuteNonQueryAsync();
  }

  /// <summary>
  /// Удаляет временную SQLite-БД.
  /// </summary>
  /// <param name="databasePath">Путь к файлу БД.</param>
  private static void DeleteDatabase(string databasePath)
  {
    SqliteConnection.ClearAllPools();

    if (File.Exists(databasePath))
      File.Delete(databasePath);
  }
}
