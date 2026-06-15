using Microsoft.Extensions.Configuration;

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
    SearchDataSourceProfileValidator sut = CreateValidator(new InMemorySearchDataSourceReader());

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
  /// Проверяет, что неизвестный provider возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public void Validate_СНеподдерживаемымProvider_ВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = CreateValidator(new InMemorySearchDataSourceReader());

    SearchDataSourceOptions source = new()
    {
      Provider = "oracle"
    };

    // Act
    ApiError? result = sut.Validate("products", source);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("DataSourceProviderNotSupported", result.Code);
  }

  /// <summary>
  /// Проверяет, что in-memory provider не требует строки подключения и SQL-запроса.
  /// </summary>
  [Fact]
  public void Validate_InMemoryПрофиль_НеТребуетConnectionStringNameИQuery()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = CreateValidator(new InMemorySearchDataSourceReader());

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
  /// Проверяет, что SQLite-профиль без имени строки подключения возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public void Validate_SqliteБезConnectionStringName_ВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = CreateValidator(new SqliteSearchDataSourceReader(CreateConfiguration()));

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
    SearchDataSourceProfileValidator sut = CreateValidator(new SqliteSearchDataSourceReader(CreateConfiguration()));

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
    SearchDataSourceProfileValidator sut = CreateValidator(new SqliteSearchDataSourceReader(CreateConfiguration()));

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
  /// Проверяет, что PostgreSQL-профиль без имени строки подключения возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public void Validate_PostgresБезConnectionStringName_ВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = CreateValidator(new PostgresSearchDataSourceReader(CreateConfiguration()));

    SearchDataSourceOptions source = new()
    {
      Provider = "postgres",
      ConnectionStringName = " ",
      Query = "select id, text from search_documents"
    };

    // Act
    ApiError? result = sut.Validate("products", source);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("DataSourceConnectionStringNameIsEmpty", result.Code);
  }

  /// <summary>
  /// Проверяет, что PostgreSQL-профиль без SQL-запроса возвращает прикладную ошибку.
  /// </summary>
  [Fact]
  public void Validate_PostgresБезQuery_ВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = CreateValidator(new PostgresSearchDataSourceReader(CreateConfiguration()));

    SearchDataSourceOptions source = new()
    {
      Provider = "postgres",
      ConnectionStringName = "POSTGRES_DEMO",
      Query = " "
    };

    // Act
    ApiError? result = sut.Validate("products", source);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("DataSourceQueryIsEmpty", result.Code);
  }

  /// <summary>
  /// Проверяет, что корректный PostgreSQL-профиль проходит проверку.
  /// </summary>
  [Fact]
  public void Validate_КорректныйPostgresПрофиль_НеВозвращаетОшибку()
  {
    // Arrange
    SearchDataSourceProfileValidator sut = CreateValidator(new PostgresSearchDataSourceReader(CreateConfiguration()));

    SearchDataSourceOptions source = new()
    {
      Provider = "postgres",
      ConnectionStringName = "POSTGRES_DEMO",
      Query = "select id, text from search_documents"
    };

    // Act
    ApiError? result = sut.Validate("products", source);

    // Assert
    Assert.Null(result);
  }

  /// <summary>
  /// Проверяет, что ошибка валидации из reader-а возвращается вызывающему коду.
  /// </summary>
  [Fact]
  public void Validate_CustomReaderСОшибкойВалидации_ВозвращаетОшибкуReader()
  {
    // Arrange
    ApiError expectedError = new()
    {
      Code = "CustomValidationError",
      Message = "Профиль custom-источника некорректен."
    };

    SearchDataSourceProfileValidator sut = CreateValidator(new TestValidationReader("custom", expectedError));

    SearchDataSourceOptions source = new()
    {
      Provider = "custom"
    };

    // Act
    ApiError? result = sut.Validate("products", source);

    // Assert
    Assert.Same(expectedError, result);
  }

  /// <summary>
  /// Создаёт валидатор с указанными reader-ами.
  /// </summary>
  /// <param name="readers">Reader-ы источников данных.</param>
  /// <returns>Валидатор профилей источников данных.</returns>
  private static SearchDataSourceProfileValidator CreateValidator(params ISearchDataSourceReader[] readers)
  {
    SearchDataSourceReaderRegistry registry = new(readers);

    return new SearchDataSourceProfileValidator(registry);
  }

  /// <summary>
  /// Создаёт пустую конфигурацию для reader-ов, которым она нужна в конструкторе.
  /// </summary>
  /// <returns>Пустая конфигурация.</returns>
  private static IConfiguration CreateConfiguration() => new ConfigurationBuilder().Build();

  /// <summary>
  /// Тестовый reader с настраиваемым результатом валидации.
  /// </summary>
  private sealed class TestValidationReader : ISearchDataSourceReader
  {
    private readonly ApiError? _validationError;

    /// <summary>
    /// Создаёт тестовый reader с настраиваемым результатом валидации.
    /// </summary>
    /// <param name="provider">Имя provider-а.</param>
    /// <param name="validationError">Ошибка валидации.</param>
    public TestValidationReader(string provider, ApiError? validationError)
    {
      Provider = provider;
      _validationError = validationError;
    }

    /// <inheritdoc />
    public string Provider { get; }

    /// <inheritdoc />
    public ApiError? ValidateProfile(
        string sourceName,
        SearchDataSourceOptions options)
    {
      return _validationError;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SearchDataSourceDocument>> ReadAsync(
        string sourceName,
        SearchDataSourceOptions options,
        CancellationToken cancellationToken = default)
    {
      return Task.FromResult<IReadOnlyList<SearchDataSourceDocument>>([]);
    }
  }
}
