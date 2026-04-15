using ResultType;

namespace SearchEngine;

/// <summary>
/// Совместимые безопасные методы поиска.
/// </summary>
/// <typeparam name="T">Тип идентификатора записи.</typeparam>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Выполняет безопасный поиск в совместимом формате результата.
  /// </summary>
  /// <param name="searchString">Поисковая строка.</param>
  /// <returns>
  /// Успешный результат со списком найденных записей или ошибка поиска.
  /// </returns>
  public Result<SearchResultList<T>, SearchEngineError> TryFind(string? searchString) =>
    TryFind(searchString, null);

  /// <summary>
  /// Выполняет безопасный поиск с параметрами запроса
  /// в совместимом формате результата.
  /// </summary>
  /// <param name="searchString">Поисковая строка.</param>
  /// <param name="request">Параметры поиска.</param>
  /// <returns>
  /// Успешный результат со списком найденных записей или ошибка поиска.
  /// </returns>
  public Result<SearchResultList<T>, SearchEngineError> TryFind(
    string? searchString,
    SearchRequest? request)
  {
    if (searchString is null)
      return Result.Failure<SearchResultList<T>, SearchEngineError>(
              SearchEngineError.NullSearchString());

    if (string.IsNullOrWhiteSpace(searchString))
      return Result.Success<SearchResultList<T>, SearchEngineError>(
              new SearchResultList<T>());

    Result<SearchResultList<T>, SearchError> result = FindResult(searchString, request);

    if (result.IsSuccess)
      return Result.Success<SearchResultList<T>, SearchEngineError>(result.Value!);

    return Result.Failure<SearchResultList<T>, SearchEngineError>(
      MapLegacyError(result.Error!));
  }

  /// <summary>
  /// Преобразует новую ошибку поиска в совместимый формат.
  /// </summary>
  /// <param name="error">Новая ошибка поиска.</param>
  /// <returns>Совместимая ошибка безопасного API.</returns>
  private static SearchEngineError MapLegacyError(SearchError error) =>
    error.Code switch
    {
      SearchErrorCode.EmptyQuery => SearchEngineError.EmptySearchString(),
      SearchErrorCode.QueryHasNoSearchableTerms => SearchEngineError.QueryHasNoSearchableTerms(),
      SearchErrorCode.IndexNotBuilt => SearchEngineError.IndexNotBuilt(),
      SearchErrorCode.IndexIsEmpty => SearchEngineError.IndexIsEmpty(),
      SearchErrorCode.InvalidSearchRequest => SearchEngineError.InvalidSearchRequest(error.Message),
      SearchErrorCode.SearchExecutionFailed => SearchEngineError.InternalError(error.Message),
      _ => SearchEngineError.InternalError(error.Message)
    };
}