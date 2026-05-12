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
}