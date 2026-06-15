namespace SearchEngine.Service;

/// <summary>
/// Настройки поискового сервиса.
/// </summary>
public sealed class SearchEngineServiceOptions
{
  /// <summary>
  /// Получает или задаёт максимальное количество документов для построения индекса.
  /// </summary>
  public int MaxDocumentCount { get; set; } = 100_000;

  /// <summary>
  /// Получает или задаёт максимальную длину текста одного документа.
  /// </summary>
  public int MaxDocumentTextLength { get; set; } = 10_000;

  /// <summary>
  /// Получает или задаёт настройки сохранения снимка индекса.
  /// </summary>
  public SearchIndexSnapshotOptions Snapshot { get; set; } = new();

  /// <summary>
  /// Получает или задаёт заранее настроенные источники данных для построения индекса.
  /// </summary>
  public Dictionary<string, SearchDataSourceOptions> Sources { get; set; } = [];
}

/// <summary>
/// Настройки сохранения снимка поискового индекса.
/// </summary>
public sealed class SearchIndexSnapshotOptions
{
  /// <summary>
  /// Получает или задаёт признак включения сохранения снимка индекса.
  /// </summary>
  public bool IsEnabled { get; set; }

  /// <summary>
  /// Получает или задаёт путь к файлу снимка индекса.
  /// </summary>
  public string FilePath { get; set; } = "data/search-index-snapshot.json";

  /// <summary>
  /// Получает или задаёт признак автоматического восстановления индекса при старте сервиса.
  /// </summary>
  public bool AutoRestoreOnStart { get; set; }
}

/// <summary>
/// Настройки источника данных для построения поискового индекса.
/// </summary>
public sealed class SearchDataSourceOptions
{
  /// <summary>
  /// Получает или задаёт признак включения источника данных.
  /// </summary>
  public bool IsEnabled { get; set; } = true;

  /// <summary>
  /// Получает или задаёт тип источника данных.
  /// </summary>
  public string Provider { get; set; } = string.Empty;

  /// <summary>
  /// Получает или задаёт документы in-memory источника данных.
  /// </summary>
  public List<SearchDataSourceDocumentOptions> Documents { get; set; } = [];

  /// <summary>
  /// Получает или задаёт имя строки подключения.
  /// </summary>
  public string ConnectionStringName { get; set; } = string.Empty;

  /// <summary>
  /// Получает или задаёт запрос для получения данных индекса.
  /// </summary>
  public string Query { get; set; } = string.Empty;

  /// <summary>
  /// Получает или задаёт таймаут выполнения SQL-команды в секундах.
  /// </summary>
  /// <remarks>
  /// Если значение не задано, используется значение provider-а ADO.NET по умолчанию.
  /// </remarks>
  public int? CommandTimeoutSeconds { get; set; }

  /// <summary>
  /// Получает или задаёт максимальное количество документов, которое reader может прочитать из источника данных.
  /// </summary>
  /// <remarks>
  /// Если значение не задано, при построении индекса из источника используется глобальный лимит
  /// <see cref="SearchEngineServiceOptions.MaxDocumentCount"/>.
  /// </remarks>
  public int? MaxReadDocumentCount { get; set; }
}

/// <summary>
/// Настройки документа in-memory источника данных.
/// </summary>
public sealed class SearchDataSourceDocumentOptions
{
  /// <summary>
  /// Получает или задаёт идентификатор документа.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  /// Получает или задаёт текст документа.
  /// </summary>
  public string Text { get; set; } = string.Empty;
}
