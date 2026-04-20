using ResultType;

namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Выполняет поиск по подготовленному индексу с использованием текущих настроек экземпляра.
  /// </summary>
  /// <param name="searchString">Поисковый запрос.</param>
  /// <returns>
  /// Успешный результат поиска либо описание ошибки.
  /// </returns>
  /// <remarks>
  /// Метод не выбрасывает исключения в ожидаемых прикладных сценариях.
  /// Для ошибок валидации и типовых сбоев возвращается объект ошибки.
  /// </remarks>
  public Result<SearchResultList<T>, SearchError> FindResult(string searchString) =>
    FindResult(searchString, null);

  /// <summary>
  /// Выполняет поиск по подготовленному индексу с параметрами конкретного запроса.
  /// </summary>
  /// <param name="searchString">Поисковый запрос.</param>
  /// <param name="request">
  /// Дополнительные параметры поиска. Если значение не задано,
  /// используются текущие настройки экземпляра.
  /// </param>
  /// <returns>
  /// Успешный результат поиска либо описание ошибки.
  /// </returns>
  /// <remarks>
  /// Метод предназначен для новых интеграций и является рекомендуемой точкой входа
  /// для выполнения поиска.
  /// </remarks>
  public Result<SearchResultList<T>, SearchError> FindResult(string searchString, SearchRequest? request)
  {
    if (string.IsNullOrWhiteSpace(searchString))
      return Result.Failure<SearchResultList<T>, SearchError>(
              new SearchError(
                SearchErrorCode.EmptyQuery,
                "Поисковый запрос пуст."));

    if (!IsIndexComplete)
      return Result.Failure<SearchResultList<T>, SearchError>(
              new SearchError(
                SearchErrorCode.IndexNotBuilt,
                "Поисковый индекс не подготовлен."));

    if (_searchIndex is null || _searchIndex.Count == 0)
      return Result.Failure<SearchResultList<T>, SearchError>(
              new SearchError(
                SearchErrorCode.IndexIsEmpty,
                "Поисковый индекс пуст."));

    if (!TryValidateRequest(request, out SearchError? validationError))
      return Result.Failure<SearchResultList<T>, SearchError>(validationError);

    try
    {
      var searchItems = DisassembleSearchTerms(searchString);

      if (searchItems.Length == 0)
        return Result.Failure<SearchResultList<T>, SearchError>(
                  new SearchError(
                    SearchErrorCode.QueryHasNoSearchableTerms,
                    "Поисковый запрос не содержит пригодных для поиска слов."));

      SearchExecutionOptions executionOptions = CreateExecutionOptions(request);
      QueryMatchMode matchMode = request?.MatchMode ?? QueryMatchMode.AllTerms;

      SearchResultList<T> searchResult = ExecuteSearch(
        searchItems,
        executionOptions,
        matchMode);

      return Result.Success<SearchResultList<T>, SearchError>(searchResult);
    }
    catch (Exception exception) when (!IsCriticalException(exception))
    {
      return Result.Failure<SearchResultList<T>, SearchError>(
        new SearchError(
          SearchErrorCode.SearchExecutionFailed,
          "Во время выполнения поиска произошла ошибка.",
          exception));
    }
  }

  /// <summary>
  /// Вычисляет допустимую дистанцию поиска по длине слова и точности.
  /// </summary>
  /// <param name="length">Длина искомого слова.</param>
  /// <param name="percent">Точность поиска в процентах.</param>
  /// <returns>Допустимая дистанция.</returns>
  private static int CalculateDistance(int length, int percent) =>
    length > 1 ? length - length * percent / 100 : 0;
}