using System.Data.Common;
using System.Globalization;

namespace SearchEngine.Service;

/// <summary>
/// Базовый reader для SQL-источников данных, которые читаются через ADO.NET.
/// </summary>
/// <remarks>
/// Класс отвечает только за общую механику SQL-чтения:
/// поиск строки подключения, выполнение заранее настроенного запроса
/// и преобразование строк результата в документы поискового индекса.
/// Конкретный provider отвечает за имя provider-а и создание подключения к своей СУБД.
/// </remarks>
public abstract class SqlQuerySearchDataSourceReader : ISearchDataSourceReader
{
  private readonly IConfiguration _configuration;
  private readonly string _providerDisplayName;

  /// <summary>
  /// Создаёт базовый SQL-reader источника данных.
  /// </summary>
  /// <param name="configuration">Конфигурация сервиса.</param>
  /// <param name="providerDisplayName">Отображаемое имя provider-а для сообщений об ошибках.</param>
  protected SqlQuerySearchDataSourceReader(
      IConfiguration configuration,
      string providerDisplayName)
  {
    ArgumentNullException.ThrowIfNull(configuration);

    if (string.IsNullOrWhiteSpace(providerDisplayName))
      throw new ArgumentException("Отображаемое имя provider-а не должно быть пустым.", nameof(providerDisplayName));

    _configuration = configuration;
    _providerDisplayName = providerDisplayName.Trim();
  }

  /// <inheritdoc />
  public abstract string Provider { get; }

  /// <inheritdoc />
  public ApiError? ValidateProfile(
      string sourceName,
      SearchDataSourceOptions options)
  {
    return SearchDataSourceProfileValidation.ValidateSqlQuerySource(
        sourceName,
        _providerDisplayName,
        options);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<SearchDataSourceDocument>> ReadAsync(
      string sourceName,
      SearchDataSourceOptions options,
      CancellationToken cancellationToken = default)
  {
    string connectionString = ResolveConnectionString(options);
    string query = ResolveQuery(sourceName, options);

    await using DbConnection connection = CreateConnection(connectionString);

    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

    using DbCommand command = connection.CreateCommand();

    command.CommandText = query;

    await using DbDataReader reader = await command
        .ExecuteReaderAsync(cancellationToken)
        .ConfigureAwait(false);

    return await ReadDocumentsAsync(sourceName, reader, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Создаёт подключение к конкретной СУБД.
  /// </summary>
  /// <param name="connectionString">Строка подключения.</param>
  /// <returns>Подключение к источнику данных.</returns>
  protected abstract DbConnection CreateConnection(string connectionString);

  /// <summary>
  /// Читает документы из результата SQL-запроса.
  /// </summary>
  /// <param name="sourceName">Имя источника данных.</param>
  /// <param name="reader">Reader результата SQL-запроса.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Документы для построения поискового индекса.</returns>
  private static async Task<IReadOnlyList<SearchDataSourceDocument>> ReadDocumentsAsync(
      string sourceName,
      DbDataReader reader,
      CancellationToken cancellationToken)
  {
    int idIndex = GetRequiredColumnOrdinal(reader, sourceName, "id");
    int textIndex = GetRequiredColumnOrdinal(reader, sourceName, "text");

    List<SearchDataSourceDocument> documents = [];

    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
      documents.Add(ReadDocument(reader, idIndex, textIndex));

    return documents;
  }

  /// <summary>
  /// Преобразует текущую строку результата SQL-запроса в документ поискового индекса.
  /// </summary>
  /// <param name="reader">Reader результата SQL-запроса.</param>
  /// <param name="idIndex">Номер колонки с идентификатором документа.</param>
  /// <param name="textIndex">Номер колонки с текстом документа.</param>
  /// <returns>Документ для построения поискового индекса.</returns>
  private static SearchDataSourceDocument ReadDocument(
      DbDataReader reader,
      int idIndex,
      int textIndex)
  {
    int id = Convert.ToInt32(
        reader.GetValue(idIndex),
        CultureInfo.InvariantCulture);

    string text = Convert.ToString(
        reader.GetValue(textIndex),
        CultureInfo.InvariantCulture) ?? string.Empty;

    return new SearchDataSourceDocument
    {
      Id = id,
      Text = text
    };
  }

  /// <summary>
  /// Возвращает индекс обязательной колонки результата SQL-запроса.
  /// </summary>
  /// <param name="reader">Reader результата SQL-запроса.</param>
  /// <param name="sourceName">Имя источника данных.</param>
  /// <param name="columnName">Имя обязательной колонки.</param>
  /// <returns>Индекс колонки в результате запроса.</returns>
  private static int GetRequiredColumnOrdinal(
      DbDataReader reader,
      string sourceName,
      string columnName)
  {
    try
    {
      return reader.GetOrdinal(columnName);
    }
    catch (IndexOutOfRangeException exception)
    {
      throw new InvalidOperationException(
          $"SQL-запрос источника данных должен возвращать колонку '{columnName}': {sourceName}.",
          exception);
    }
  }

  /// <summary>
  /// Возвращает строку подключения из конфигурации сервиса.
  /// </summary>
  /// <param name="options">Настройки источника данных.</param>
  /// <returns>Строка подключения.</returns>
  private string ResolveConnectionString(SearchDataSourceOptions options)
  {
    if (string.IsNullOrWhiteSpace(options.ConnectionStringName))
      throw new InvalidOperationException(
          $"Для {_providerDisplayName}-источника не задано имя строки подключения.");

    string connectionStringName = options.ConnectionStringName.Trim();

    string? connectionString =
        _configuration.GetConnectionString(connectionStringName);

    if (string.IsNullOrWhiteSpace(connectionString))
      connectionString = _configuration[connectionStringName];

    if (string.IsNullOrWhiteSpace(connectionString))
      throw new InvalidOperationException(
          $"Строка подключения не найдена для {_providerDisplayName}-источника: {connectionStringName}.");

    return connectionString;
  }

  /// <summary>
  /// Возвращает SQL-запрос источника данных.
  /// </summary>
  /// <param name="sourceName">Имя источника данных.</param>
  /// <param name="options">Настройки источника данных.</param>
  /// <returns>SQL-запрос источника данных.</returns>
  private string ResolveQuery(
      string sourceName,
      SearchDataSourceOptions options)
  {
    if (string.IsNullOrWhiteSpace(options.Query))
      throw new InvalidOperationException(
          $"Для {_providerDisplayName}-источника не задан SQL-запрос: {sourceName}.");

    return options.Query;
  }
}
