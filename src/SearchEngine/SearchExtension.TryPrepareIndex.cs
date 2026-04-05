using ResultType;

namespace SearchEngine;

/// <summary>
/// Совместимые безопасные методы подготовки индекса.
/// </summary>
public static partial class SearchExtension
{
  /// <summary>
  /// Безопасно подготавливает индекс из коллекции объектов-источников.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Источник данных.</param>
  /// <param name="delimiters">Пользовательский набор разделителей слов.</param>
  /// <returns>
  /// Успешный результат, если индекс подготовлен;
  /// иначе совместимая ошибка безопасного API.
  /// </returns>
  public static Task<UnitResult<SearchEngineError>> TryPrepareIndex<T>(
    this Search<T>? search,
    IEnumerable<ISourceData<T>>? source,
    string? delimiters = null)
    where T : struct
  {
    if (search is null)
      return Task.FromResult(
              UnitResult.Failure(SearchEngineError.SearchIsNull()));

    if (source is null)
      return Task.FromResult(
              UnitResult.Failure(SearchEngineError.SourceIsNull()));

    ISourceData<T>[] materializedSource = source as ISourceData<T>[] ?? source.ToArray();

    if (materializedSource.Length == 0)
      return Task.FromResult(
              UnitResult.Failure(SearchEngineError.SourceIsEmpty()));

    return TryPrepareIndexInternal(() => search.PrepareIndexResult(materializedSource, delimiters));
  }

  /// <summary>
  /// Безопасно подготавливает индекс из массива строк.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Источник данных.</param>
  /// <param name="elementDelimiter">Разделитель между идентификатором и текстом.</param>
  /// <param name="delimiters">Пользовательский набор разделителей слов.</param>
  /// <param name="forceParallel">Признак принудительного включения параллельной обработки.</param>
  /// <param name="parallelProcessingThreshold">Порог количества элементов для включения параллельной обработки.</param>
  /// <returns>
  /// Успешный результат, если индекс подготовлен;
  /// иначе совместимая ошибка безопасного API.
  /// </returns>
  public static Task<UnitResult<SearchEngineError>> TryPrepareIndex<T>(
    this Search<T>? search,
    string[]? source,
    string? elementDelimiter,
    string? delimiters = null,
    bool forceParallel = false,
    int parallelProcessingThreshold = 10_000)
    where T : struct
  {
    if (search is null)
      return Task.FromResult(
              UnitResult.Failure(SearchEngineError.SearchIsNull()));

    if (source is null)
      return Task.FromResult(
              UnitResult.Failure(SearchEngineError.SourceIsNull()));

    if (source.Length == 0)
      return Task.FromResult(
              UnitResult.Failure(SearchEngineError.SourceIsEmpty()));

    if (string.IsNullOrWhiteSpace(elementDelimiter))
      return Task.FromResult(
              UnitResult.Failure(SearchEngineError.ElementDelimiterIsEmpty()));

    return TryPrepareIndexInternal(() => search.PrepareIndexResult(
      source,
      elementDelimiter,
      delimiters,
      forceParallel,
      parallelProcessingThreshold));
  }

  /// <summary>
  /// Выполняет безопасную подготовку индекса и приводит новую ошибку
  /// к совместимому формату безопасного API.
  /// </summary>
  /// <param name="action">Операция подготовки индекса.</param>
  /// <returns>
  /// Успешный результат либо совместимая ошибка безопасного API.
  /// </returns>
  private static async Task<UnitResult<SearchEngineError>> TryPrepareIndexInternal(
    Func<Task<UnitResult<SearchError>>> action)
  {
    UnitResult<SearchError> result = await action().ConfigureAwait(false);

    if (result.IsSuccess)
      return UnitResult.Success<SearchEngineError>();

    return UnitResult.Failure(MapLegacyError(result.Error!));
  }

  /// <summary>
  /// Преобразует новую ошибку индексации в совместимый формат.
  /// </summary>
  /// <param name="error">Новая ошибка индексации.</param>
  /// <returns>Совместимая ошибка безопасного API.</returns>
  private static SearchEngineError MapLegacyError(SearchError error) =>
    error.Code switch
    {
      SearchErrorCode.InvalidSourceRecord => SearchEngineError.IndexBuildFailed(error.Message),
      SearchErrorCode.InvalidDelimitedSourceFormat => SearchEngineError.IndexBuildFailed(error.Message),
      SearchErrorCode.InvalidIdFormat => SearchEngineError.IndexBuildFailed(error.Message),
      SearchErrorCode.IndexBuildFailed => SearchEngineError.IndexBuildFailed(error.Message),
      _ => SearchEngineError.InternalError(error.Message)
    };
}