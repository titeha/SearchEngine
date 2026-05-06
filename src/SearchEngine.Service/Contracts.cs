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
/// Запрос на выполнение поиска по текущему индексу.
/// </summary>
public sealed record SearchQueryRequest
{
  /// <summary>
  /// Получает поисковую строку.
  /// </summary>
  public string? Query { get; init; }

  /// <summary>
  /// Получает тип поиска.
  /// </summary>
  public SearchType SearchType { get; init; } = SearchType.ExactSearch;

  /// <summary>
  /// Получает место поиска внутри слова.
  /// </summary>
  public SearchLocation SearchLocation { get; init; } = SearchLocation.BeginWord;
}

/// <summary>
/// Ответ на поисковый запрос.
/// </summary>
public sealed record SearchQueryResponse
{
  /// <summary>
  /// Получает признак наличия найденных документов.
  /// </summary>
  public bool IsHasIndex { get; init; }

  /// <summary>
  /// Получает найденные документы, сгруппированные по ключу результата.
  /// </summary>
  public IReadOnlyList<SearchResultBucket> Items { get; init; } = [];
}

/// <summary>
/// Группа найденных документов с одинаковым ключом результата.
/// </summary>
public sealed record SearchResultBucket
{
  /// <summary>
  /// Получает ключ группы результата.
  /// </summary>
  public int Key { get; init; }

  /// <summary>
  /// Получает идентификаторы найденных документов.
  /// </summary>
  public IReadOnlyList<int> Ids { get; init; } = [];
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