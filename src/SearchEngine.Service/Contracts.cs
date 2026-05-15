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
  /// Получает режим объединения результатов по словам поискового запроса.
  /// </summary>
  public QueryMatchMode MatchMode { get; init; } = QueryMatchMode.AllTerms;

  /// <summary>
  /// Получает тип поиска.
  /// </summary>
  public SearchType SearchType { get; init; } = SearchType.ExactSearch;

  /// <summary>
  /// Получает место поиска внутри слова.
  /// </summary>
  public SearchLocation SearchLocation { get; init; } = SearchLocation.BeginWord;

  /// <summary>
  /// Получает точность нечёткого поиска в процентах.
  /// </summary>
  public int? PrecisionSearch { get; init; }

  /// <summary>
  /// Получает допустимое количество опечаток.
  /// </summary>
  public int? AcceptableCountMisprint { get; init; }
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
/// Ответ со справочниками допустимых параметров поиска.
/// </summary>
public sealed record SearchOptionsResponse
{
  /// <summary>
  /// Получает допустимые режимы объединения слов запроса.
  /// </summary>
  public string[] MatchModes { get; init; } = [];

  /// <summary>
  /// Получает допустимые типы поиска.
  /// </summary>
  public string[] SearchTypes { get; init; } = [];

  /// <summary>
  /// Получает допустимые места поиска внутри слова.
  /// </summary>
  public string[] SearchLocations { get; init; } = [];

  /// <summary>
  /// Получает режим объединения слов запроса по умолчанию.
  /// </summary>
  public string DefaultMatchMode { get; init; } = string.Empty;

  /// <summary>
  /// Получает тип поиска по умолчанию.
  /// </summary>
  public string DefaultSearchType { get; init; } = string.Empty;

  /// <summary>
  /// Получает место поиска внутри слова по умолчанию.
  /// </summary>
  public string DefaultSearchLocation { get; init; } = string.Empty;
}

/// <summary>
/// Ответ проверки готовности сервиса к поиску.
/// </summary>
public sealed record ReadinessResponse
{
  /// <summary>
  /// Получает состояние готовности сервиса.
  /// </summary>
  public string Status { get; init; } = string.Empty;

  /// <summary>
  /// Получает признак готовности индекса к поиску.
  /// </summary>
  public bool IsReady { get; init; }

  /// <summary>
  /// Получает количество документов в текущем индексе.
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
/// Ответ с активными настройками поискового сервиса.
/// </summary>
public sealed record SearchEngineServiceConfigResponse
{
  /// <summary>
  /// Получает максимальное количество документов для построения индекса.
  /// </summary>
  public int MaxDocumentCount { get; init; }

  /// <summary>
  /// Получает максимальную длину текста одного документа.
  /// </summary>
  public int MaxDocumentTextLength { get; init; }

  /// <summary>
  /// Получает настройки снимка поискового индекса.
  /// </summary>
  public SearchIndexSnapshotConfigResponse Snapshot { get; init; } = new();
}

/// <summary>
/// Ответ с настройками снимка поискового индекса.
/// </summary>
public sealed record SearchIndexSnapshotConfigResponse
{
  /// <summary>
  /// Получает признак включения сохранения снимка индекса.
  /// </summary>
  public bool IsEnabled { get; init; }

  /// <summary>
  /// Получает признак автоматического восстановления индекса при старте сервиса.
  /// </summary>
  public bool AutoRestoreOnStart { get; init; }

  /// <summary>
  /// Получает путь к файлу снимка индекса.
  /// </summary>
  public string FilePath { get; init; } = string.Empty;
}

/// <summary>
/// Ответ со списком настроенных источников данных.
/// </summary>
public sealed record SearchDataSourcesResponse
{
  /// <summary>
  /// Получает поддерживаемые provider-ы источников данных.
  /// </summary>
  public IReadOnlyList<string> SupportedProviders { get; init; } = [];

  /// <summary>
  /// Получает безопасное описание источников данных.
  /// </summary>
  public IReadOnlyList<SearchDataSourceResponse> Items { get; init; } = [];
}

/// <summary>
/// Безопасное описание источника данных.
/// </summary>
public sealed record SearchDataSourceResponse
{
  /// <summary>
  /// Получает имя источника данных.
  /// </summary>
  public string Name { get; init; } = string.Empty;

  /// <summary>
  /// Получает признак включения источника данных.
  /// </summary>
  public bool IsEnabled { get; init; }

  /// <summary>
  /// Получает признак поддержки provider-а источника данных текущим сервисом.
  /// </summary>
  public bool IsProviderSupported { get; init; }

  /// <summary>
  /// Получает тип источника данных.
  /// </summary>
  public string Provider { get; init; } = string.Empty;

  /// <summary>
  /// Получает признак наличия имени строки подключения.
  /// </summary>
  public bool HasConnectionStringName { get; init; }

  /// <summary>
  /// Получает признак наличия запроса для получения данных.
  /// </summary>
  public bool HasQuery { get; init; }
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