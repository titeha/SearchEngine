namespace SearchEngine;

/// <summary>
/// Коды ошибок, которые может возвращать поисковый движок.
/// </summary>
public enum SearchErrorCode
{
  /// <summary>
  /// Поисковый запрос пуст.
  /// </summary>
  EmptyQuery,

  /// <summary>
  /// В запросе не осталось слов, пригодных для поиска.
  /// </summary>
  QueryHasNoSearchableTerms,

  /// <summary>
  /// Поиск запущен до построения индекса.
  /// </summary>
  IndexNotBuilt,

  /// <summary>
  /// Индекс построен, но не содержит данных для поиска.
  /// </summary>
  IndexIsEmpty,

  /// <summary>
  /// Переданы недопустимые параметры поиска.
  /// </summary>
  InvalidSearchOptions,

  /// <summary>
  /// Передан недопустимый объект запроса.
  /// </summary>
  InvalidSearchRequest,

  /// <summary>
  /// Обнаружена некорректная запись исходных данных.
  /// </summary>
  InvalidSourceRecord,

  /// <summary>
  /// Обнаружен некорректный формат источника с разделителями.
  /// </summary>
  InvalidDelimitedSourceFormat,

  /// <summary>
  /// Не удалось преобразовать идентификатор записи.
  /// </summary>
  InvalidIdFormat,

  /// <summary>
  /// Повторно запущено ручное построение индекса.
  /// </summary>
  ManualIndexingAlreadyStarted,

  /// <summary>
  /// Запрошено завершение ручного построения индекса до его начала.
  /// </summary>
  ManualIndexingNotStarted,

  /// <summary>
  /// Не удалось инициализировать внутренние ресурсы библиотеки.
  /// </summary>
  ResourceInitializationFailed,

  /// <summary>
  /// Произошла ошибка при построении индекса.
  /// </summary>
  IndexBuildFailed,

  /// <summary>
  /// Произошла ошибка при выполнении поиска.
  /// </summary>
  SearchExecutionFailed
}
