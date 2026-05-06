namespace SearchEngine.Service;

/// <summary>
/// Запрос на проверку документов перед построением поискового индекса.
/// </summary>
public sealed record IndexBuildRequest
{
  /// <summary>
  /// Получает признак включения фонетического поиска.
  /// </summary>
  public bool IsPhoneticSearch { get; init; }

  /// <summary>
  /// Получает документы, которые планируется добавить в поисковый индекс.
  /// </summary>
  public IReadOnlyList<IndexDocumentRequest?>? Documents { get; init; }
}

/// <summary>
/// Документ, передаваемый в сервис для последующей индексации.
/// </summary>
public sealed record IndexDocumentRequest
{
  /// <summary>
  /// Получает идентификатор документа.
  /// </summary>
  public int Id { get; init; }

  /// <summary>
  /// Получает текст документа для поиска.
  /// </summary>
  public string? Text { get; init; }
}

/// <summary>
/// Ответ проверки документов перед построением поискового индекса.
/// </summary>
public sealed record IndexValidateResponse
{
  /// <summary>
  /// Получает общее количество переданных документов.
  /// </summary>
  public int DocumentCount { get; init; }

  /// <summary>
  /// Получает количество документов с непустым текстом.
  /// </summary>
  public int SearchableDocumentCount { get; init; }

  /// <summary>
  /// Получает признак включения фонетического поиска.
  /// </summary>
  public bool IsPhoneticSearch { get; init; }
}

/// <summary>
/// Текущее состояние поискового индекса.
/// </summary>
public sealed record IndexStatusResponse
{
  /// <summary>
  /// Получает признак готовности индекса к поиску.
  /// </summary>
  public bool IsReady { get; init; }

  /// <summary>
  /// Получает количество документов в индексе.
  /// </summary>
  public int DocumentCount { get; init; }

  /// <summary>
  /// Получает количество документов с непустым текстом.
  /// </summary>
  public int SearchableDocumentCount { get; init; }

  /// <summary>
  /// Получает признак включения фонетического поиска.
  /// </summary>
  public bool IsPhoneticSearch { get; init; }

  /// <summary>
  /// Получает дату и время построения индекса в UTC.
  /// </summary>
  public DateTimeOffset? CreatedAtUtc { get; init; }
}

/// <summary>
/// Ошибка API сервиса.
/// </summary>
public sealed record ApiError
{
  /// <summary>
  /// Получает код ошибки.
  /// </summary>
  public string Code { get; init; } = string.Empty;

  /// <summary>
  /// Получает сообщение ошибки.
  /// </summary>
  public string Message { get; init; } = string.Empty;
}