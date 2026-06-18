using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты публичного API регистрации пользовательских reader-ов источников данных.
/// </summary>
public sealed class SearchDataSourceReaderExtensionsTests
{
  /// <summary>
  /// Проверяет, что пользовательский reader попадает в registry через DI.
  /// </summary>
  [Fact]
  public void AddSearchDataSourceReader_РегистрируетReaderВRegistry()
  {
    // Arrange
    ServiceCollection services = new();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddSearchDataSourceReader<InMemorySearchDataSourceReader>();
    services.AddSingleton<SearchDataSourceReaderRegistry>();

    using ServiceProvider provider = services.BuildServiceProvider();

    // Act
    SearchDataSourceReaderRegistry registry = provider.GetRequiredService<SearchDataSourceReaderRegistry>();

    // Assert
    Assert.True(registry.IsSupported(InMemorySearchDataSourceReader.ProviderName));
  }

  /// <summary>
  /// Проверяет, что SQL-источник, заданный фабрикой подключения, попадает в registry как delegate-reader.
  /// </summary>
  [Fact]
  public void AddSqlSearchDataSource_РегистрируетDelegateReaderВRegistry()
  {
    // Arrange
    ServiceCollection services = new();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    services.AddSqlSearchDataSource("db2", "IBM DB2", connectionString => new SqliteConnection(connectionString));
    services.AddSingleton<SearchDataSourceReaderRegistry>();

    using ServiceProvider provider = services.BuildServiceProvider();

    // Act
    SearchDataSourceReaderRegistry registry = provider.GetRequiredService<SearchDataSourceReaderRegistry>();

    // Assert
    Assert.True(registry.IsSupported("db2"));
    Assert.IsType<DelegateSqlSearchDataSourceReader>(registry.GetReader("db2"));
  }

  /// <summary>
  /// Проверяет, что delegate-reader читает документы через переданную фабрику подключения.
  /// </summary>
  /// <remarks>
  /// SQLite здесь играет роль «экзотической» СУБД, подключённой только через фабрику,
  /// без отдельного класса-наследника.
  /// </remarks>
  [Fact]
  public async Task DelegateReader_ЧитаетДокументыЧерезФабрикуПодключения()
  {
    // Arrange
    string databasePath = CreateDatabasePath();

    try
    {
      string connectionString = CreateConnectionString(databasePath);

      await CreateDemoDatabaseAsync(connectionString);

      IConfiguration configuration = CreateConfiguration("ConnectionStrings:CUSTOM_DEMO", connectionString);

      DelegateSqlSearchDataSourceReader sut = new(
          configuration,
          "custom",
          "Custom SQL",
          factoryConnectionString => new SqliteConnection(factoryConnectionString));

      SearchDataSourceOptions options = new()
      {
        ConnectionStringName = "CUSTOM_DEMO",
        Query = "select id, text from search_documents order by id"
      };

      // Act
      IReadOnlyList<SearchDataSourceDocument> result = await sut.ReadAsync("custom-demo", options);

      // Assert
      Assert.Equal("custom", sut.Provider);
      Assert.Equal(2, result.Count);
      Assert.Equal("Иванов Сергей Петрович", result[0].Text);
    }
    finally
    {
      DeleteDatabase(databasePath);
    }
  }

  /// <summary>
  /// Проверяет, что delegate-reader использует общую валидацию SQL-профиля.
  /// </summary>
  [Fact]
  public void DelegateReader_ValidateProfile_БезСтрокиПодключения_ВозвращаетОшибку()
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder().Build();

    DelegateSqlSearchDataSourceReader sut = new(
        configuration,
        "custom",
        "Custom SQL",
        connectionString => new SqliteConnection(connectionString));

    SearchDataSourceOptions options = new()
    {
      Query = "select 1 as id, 'Тестовый документ' as text"
    };

    // Act
    ApiError? error = sut.ValidateProfile("custom-demo", options);

    // Assert
    Assert.NotNull(error);
    Assert.Equal("DataSourceConnectionStringNameIsEmpty", error!.Code);
  }

  /// <summary>
  /// Проверяет, что пустое имя provider-а недопустимо при регистрации SQL-источника.
  /// </summary>
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  public void AddSqlSearchDataSource_СПустымProvider_ВыбрасываетИсключение(string provider)
  {
    // Arrange
    ServiceCollection services = new();

    // Act
    void Act() => services.AddSqlSearchDataSource(provider, "Custom SQL", connectionString => new SqliteConnection(connectionString));

    // Assert
    Assert.Throws<ArgumentException>(Act);
  }

  /// <summary>
  /// Создаёт путь к временной SQLite-БД.
  /// </summary>
  /// <returns>Путь к временной SQLite-БД.</returns>
  private static string CreateDatabasePath()
  {
    return Path.Combine(
        Path.GetTempPath(),
        $"search-engine-service-delegate-{Guid.NewGuid():N}.db");
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
