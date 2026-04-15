namespace SearchEngine;

/// <summary>
/// Коды ошибок совместимого безопасного API поиска.
/// </summary>
public enum SearchEngineErrorCode
{
  /// <summary>
  /// Поисковая строка равна <see langword="null"/>.
  /// </summary>
  NullSearchString,

  /// <summary>
  /// Поисковая строка пуста или содержит только пробельные символы.
  /// </summary>
  EmptySearchString,

  /// <summary>
  /// В запросе нет пригодных для поиска слов.
  /// </summary>
  QueryHasNoSearchableTerms,

  /// <summary>
  /// Поисковый индекс ещё не подготовлен.
  /// </summary>
  IndexNotBuilt,

  /// <summary>
  /// Поисковый индекс пуст.
  /// </summary>
  IndexIsEmpty,

  /// <summary>
  /// Параметры поискового запроса некорректны.
  /// </summary>
  InvalidSearchRequest,

  /// <summary>
  /// Внутренняя ошибка во время выполнения поиска.
  /// </summary>
  InternalError,

  /// <summary>
  /// Экземпляр поиска не задан.
  /// </summary>
  SearchIsNull,

  /// <summary>
  /// Источник данных не задан.
  /// </summary>
  SourceIsNull,

  /// <summary>
  /// Источник данных пуст.
  /// </summary>
  SourceIsEmpty,

  /// <summary>
  /// Не задан разделитель между идентификатором и текстом.
  /// </summary>
  ElementDelimiterIsEmpty,

  /// <summary>
  /// Ошибка при построении индекса.
  /// </summary>
  IndexBuildFailed
}

/// <summary>
/// Ошибка, возвращаемая совместимыми безопасными методами библиотеки поиска.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр ошибки безопасного API.
/// </remarks>
/// <param name="code">Код ошибки.</param>
/// <param name="message">Описание ошибки.</param>
public readonly struct SearchEngineError(SearchEngineErrorCode code, string message)
{  /// <summary>
  /// Код ошибки.
  /// </summary>
  public SearchEngineErrorCode Code { get; } = code;

  /// <summary>
  /// Описание ошибки.
  /// </summary>
  public string Message { get; } = message;

  /// <inheritdoc />
  public override string ToString() => $"{Code}: {Message}";

  /// <summary>
  /// Создаёт ошибку для <see langword="null"/> поисковой строки.
  /// </summary>
  public static SearchEngineError NullSearchString() =>
    new(
      SearchEngineErrorCode.NullSearchString,
      "Поисковая строка не может быть null.");

  /// <summary>
  /// Создаёт ошибку для пустой поисковой строки.
  /// </summary>
  public static SearchEngineError EmptySearchString() =>
    new(
      SearchEngineErrorCode.EmptySearchString,
      "Поисковая строка пуста или состоит только из пробельных символов.");

  /// <summary>
  /// Создаёт ошибку для запроса без пригодных для поиска слов.
  /// </summary>
  public static SearchEngineError QueryHasNoSearchableTerms() =>
    new(
      SearchEngineErrorCode.QueryHasNoSearchableTerms,
      "Поисковый запрос не содержит пригодных для поиска слов.");

  /// <summary>
  /// Создаёт ошибку для неподготовленного индекса.
  /// </summary>
  public static SearchEngineError IndexNotBuilt() =>
    new(
      SearchEngineErrorCode.IndexNotBuilt,
      "Индекс ещё не подготовлен. Выполните PrepareIndex перед поиском.");

  /// <summary>
  /// Создаёт ошибку для пустого индекса.
  /// </summary>
  public static SearchEngineError IndexIsEmpty() =>
    new(
      SearchEngineErrorCode.IndexIsEmpty,
      "Поисковый индекс пуст.");

  /// <summary>
  /// Создаёт ошибку для некорректных параметров поиска.
  /// </summary>
  /// <param name="message">Описание ошибки.</param>
  public static SearchEngineError InvalidSearchRequest(string message) =>
    new(
      SearchEngineErrorCode.InvalidSearchRequest,
      message);

  /// <summary>
  /// Создаёт внутреннюю ошибку поиска.
  /// </summary>
  /// <param name="message">Описание ошибки.</param>
  public static SearchEngineError InternalError(string message) =>
    new(
      SearchEngineErrorCode.InternalError,
      message);

  /// <summary>
  /// Создаёт ошибку для отсутствующего экземпляра поиска.
  /// </summary>
  public static SearchEngineError SearchIsNull() =>
    new(
      SearchEngineErrorCode.SearchIsNull,
      "Экземпляр поиска не задан.");

  /// <summary>
  /// Создаёт ошибку для отсутствующего источника данных.
  /// </summary>
  public static SearchEngineError SourceIsNull() =>
    new(
      SearchEngineErrorCode.SourceIsNull,
      "Источник данных не задан.");

  /// <summary>
  /// Создаёт ошибку для пустого источника данных.
  /// </summary>
  public static SearchEngineError SourceIsEmpty() =>
    new(
      SearchEngineErrorCode.SourceIsEmpty,
      "Источник данных пуст или не содержит элементов.");

  /// <summary>
  /// Создаёт ошибку для пустого разделителя элементов.
  /// </summary>
  public static SearchEngineError ElementDelimiterIsEmpty() =>
    new(
      SearchEngineErrorCode.ElementDelimiterIsEmpty,
      "Не задан разделитель между идентификатором и текстом.");

  /// <summary>
  /// Создаёт ошибку построения индекса.
  /// </summary>
  /// <param name="message">Описание ошибки.</param>
  public static SearchEngineError IndexBuildFailed(string message) =>
    new(
      SearchEngineErrorCode.IndexBuildFailed,
      message);
}