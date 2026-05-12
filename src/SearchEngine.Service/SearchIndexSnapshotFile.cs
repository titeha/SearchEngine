namespace SearchEngine.Service;

/// <summary>
/// Файловый снимок данных поискового индекса.
/// </summary>
public sealed record SearchIndexSnapshotFile
{
  /// <summary>
  /// Получает версию формата snapshot-файла.
  /// </summary>
  public int Version { get; init; } = 1;

  /// <summary>
  /// Получает признак включения фонетического поиска.
  /// </summary>
  public bool IsPhoneticSearch { get; init; }

  /// <summary>
  /// Получает дату и время создания snapshot-файла в UTC.
  /// </summary>
  public DateTimeOffset CreatedAtUtc { get; init; }

  /// <summary>
  /// Получает документы, из которых можно восстановить поисковый индекс.
  /// </summary>
  public List<SearchIndexSnapshotDocument> Documents { get; init; } = [];
}

/// <summary>
/// Документ, сохранённый в snapshot-файле поискового индекса.
/// </summary>
public sealed record SearchIndexSnapshotDocument
{
  /// <summary>
  /// Получает идентификатор документа.
  /// </summary>
  public int Id { get; init; }

  /// <summary>
  /// Получает текст документа.
  /// </summary>
  public string Text { get; init; } = string.Empty;
}