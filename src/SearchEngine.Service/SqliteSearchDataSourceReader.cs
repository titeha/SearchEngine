using System.Globalization;

using Microsoft.Data.Sqlite;

namespace SearchEngine.Service;

/// <summary>
/// Читает документы из SQLite-источника данных.
/// </summary>
/// <remarks>
/// Создаёт reader SQLite-источника данных.
/// </remarks>
/// <param name="configuration">Конфигурация сервиса.</param>
public sealed class SqliteSearchDataSourceReader(IConfiguration configuration) : ISearchDataSourceReader
{
  /// <summary>
  /// Имя provider-а SQLite.
  /// </summary>
  public const string ProviderName = "sqlite";

  private readonly IConfiguration _configuration = configuration;

  /// <inheritdoc />
  public string Provider => ProviderName;

  /// <inheritdoc />
  public ApiError? ValidateProfile(
      string sourceName,
      SearchDataSourceOptions options)
  {
    return SearchDataSourceProfileValidation.ValidateSqlQuerySource(
        sourceName,
        "SQLite",
        options);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<SearchDataSourceDocument>> ReadAsync(
      string sourceName,
      SearchDataSourceOptions options,
      CancellationToken cancellationToken = default)
  {
    string connectionString = ResolveConnectionString(options);

    if (string.IsNullOrWhiteSpace(options.Query))
      throw new InvalidOperationException($"Для SQLite-источника не задан SQL-запрос: {sourceName}.");

    List<SearchDataSourceDocument> documents = [];

    await using SqliteConnection connection = new(connectionString);

    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

    await using SqliteCommand command = connection.CreateCommand();

    command.CommandText = options.Query;

    await using SqliteDataReader reader = await command
        .ExecuteReaderAsync(cancellationToken)
        .ConfigureAwait(false);

    int idIndex = reader.GetOrdinal("id");
    int textIndex = reader.GetOrdinal("text");

    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
    {
      int id = Convert.ToInt32(
          reader.GetValue(idIndex),
          CultureInfo.InvariantCulture);

      string text = Convert.ToString(
          reader.GetValue(textIndex),
          CultureInfo.InvariantCulture) ?? string.Empty;

      documents.Add(new SearchDataSourceDocument
      {
        Id = id,
        Text = text
      });
    }

    return documents;
  }

  /// <summary>
  /// Возвращает строку подключения SQLite.
  /// </summary>
  /// <param name="options">Настройки источника данных.</param>
  /// <returns>Строка подключения SQLite.</returns>
  private string ResolveConnectionString(SearchDataSourceOptions options)
  {
    if (string.IsNullOrWhiteSpace(options.ConnectionStringName))
      throw new InvalidOperationException("Для SQLite-источника не задано имя строки подключения.");

    string connectionStringName = options.ConnectionStringName.Trim();

    string? connectionString =
        _configuration.GetConnectionString(connectionStringName);

    if (string.IsNullOrWhiteSpace(connectionString))
      connectionString = _configuration[connectionStringName];

    if (string.IsNullOrWhiteSpace(connectionString))
      throw new InvalidOperationException($"Строка подключения не найдена: {connectionStringName}.");

    return connectionString;
  }
}