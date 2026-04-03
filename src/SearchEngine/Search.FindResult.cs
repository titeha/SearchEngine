using ResultType;

using StringFunctions;

namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
/// <typeparam name="T">Тип идентификатора записи.</typeparam>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Выполняет поиск и возвращает результат без выбрасывания исключений
  /// в ожидаемых прикладных сценариях.
  /// </summary>
  /// <param name="searchString">Поисковый запрос.</param>
  /// <returns>
  /// Успешный результат поиска либо описание ошибки.
  /// </returns>
  public Result<SearchResultList<T>, SearchError> FindResult(string searchString) =>
    FindResult(searchString, null);

  /// <summary>
  /// Выполняет поиск с параметрами конкретного запроса и возвращает результат
  /// без выбрасывания исключений в ожидаемых прикладных сценариях.
  /// </summary>
  /// <param name="searchString">Поисковый запрос.</param>
  /// <param name="request">Параметры поиска.</param>
  /// <returns>
  /// Успешный результат поиска либо описание ошибки.
  /// </returns>
  public Result<SearchResultList<T>, SearchError> FindResult(string searchString, SearchRequest? request)
  {
    if (searchString.IsNullOrWhiteSpace())
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
      return Result.Failure<SearchResultList<T>, SearchError>(validationError!);

    try
    {
      DisassemblyString(searchString);

      if (_searchList.Count == 0)
      {
        return Result.Failure<SearchResultList<T>, SearchError>(
          new SearchError(
            SearchErrorCode.QueryHasNoSearchableTerms,
            "Поисковый запрос не содержит пригодных для поиска слов."));
      }

      SearchType searchType = request?.SearchType ?? SearchType;
      SearchLocation searchLocation = request?.SearchLocation ?? SearchLocation;
      int precisionSearch = request?.PrecisionSearch ?? PrecisionSearch;
      int acceptableCountMisprint = request?.AcceptableCountMisprint ?? AcceptableCountMisprint;
      QueryMatchMode matchMode = request?.MatchMode ?? QueryMatchMode.AllTerms;

      SearchResultList<T> searchResult = ExecuteSearch(
        _searchList,
        searchType,
        searchLocation,
        precisionSearch,
        acceptableCountMisprint,
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
  /// Выполняет поиск по подготовленному набору слов.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <param name="searchType">Тип поискового механизма.</param>
  /// <param name="searchLocation">Место поиска.</param>
  /// <param name="precisionSearch">Точность поиска в процентах.</param>
  /// <param name="acceptableCountMisprint">Допустимое количество опечаток.</param>
  /// <param name="matchMode">Режим объединения слов запроса.</param>
  /// <returns>Результат поиска.</returns>
  private SearchResultList<T> ExecuteSearch(
    IEnumerable<string> searchItems,
    SearchType searchType,
    SearchLocation searchLocation,
    int precisionSearch,
    int acceptableCountMisprint,
    QueryMatchMode matchMode)
  {
    SearchLocation originalSearchLocation = SearchLocation;
    int originalPrecisionSearch = PrecisionSearch;
    int originalAcceptableCountMisprint = AcceptableCountMisprint;
    SearchType originalSearchType = SearchType;

    try
    {
      SearchLocation = searchLocation;
      PrecisionSearch = precisionSearch;
      AcceptableCountMisprint = acceptableCountMisprint;
      SearchType = searchType;

      return matchMode switch
      {
        QueryMatchMode.AllTerms => ExecuteAllTermsSearch(searchItems),
        QueryMatchMode.AnyTerm => ExecuteAnyTermSearch(searchItems),
        _ => throw new InvalidOperationException(
          $"Режим {matchMode} не поддерживается текущей реализацией поиска.")
      };
    }
    finally
    {
      SearchLocation = originalSearchLocation;
      PrecisionSearch = originalPrecisionSearch;
      AcceptableCountMisprint = originalAcceptableCountMisprint;
      SearchType = originalSearchType;
    }
  }

  /// <summary>
  /// Выполняет поиск в режиме строгого совпадения всех слов запроса.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <returns>Результат поиска.</returns>
  private SearchResultList<T> ExecuteAllTermsSearch(IEnumerable<string> searchItems)
  {
    SearchResultList<T> searchResult = new();

    foreach (string item in searchItems)
      searchResult.Union(ExecuteSingleItemSearch(item));

    return searchResult;
  }

  /// <summary>
  /// Выполняет поиск в режиме совпадения по любому слову запроса.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <returns>Результат поиска.</returns>
  private SearchResultList<T> ExecuteAnyTermSearch(IEnumerable<string> searchItems)
  {
    Dictionary<T, int> bestDistances = new();

    foreach (string item in searchItems)
    {
      SearchResultList<T> itemResult = ExecuteSingleItemSearch(item);

      foreach (var bucket in itemResult.Items)
      {
        int distance = bucket.Key;

        foreach (T index in bucket.Value.Items)
          if (!bestDistances.TryGetValue(index, out int currentDistance) || distance < currentDistance)
            bestDistances[index] = distance;
      }
    }

    return BuildSearchResult(bestDistances);
  }

  /// <summary>
  /// Выполняет поиск по одному слову.
  /// </summary>
  /// <param name="item">Искомое слово.</param>
  /// <returns>Результат поиска по одному слову.</returns>
  private SearchResultList<T> ExecuteSingleItemSearch(string item)
  {
    if (IsPhoneticSearch)
      return PhoneticFind(item);

    if (item.Length == 2 || SearchType.ExactSearch == SearchType)
      return ExactSearch(item);

    int distance = AcceptableCountMisprint >= 0
      ? AcceptableCountMisprint
      : CalculateDistance(item.Length, PrecisionSearch);

    return FusySearch(item, distance);
  }

  /// <summary>
  /// Создаёт результат поиска из набора лучших дистанций по найденным идентификаторам.
  /// </summary>
  /// <param name="bestDistances">
  /// Набор идентификаторов и их лучших дистанций.
  /// </param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildSearchResult(IReadOnlyDictionary<T, int> bestDistances)
  {
    SearchResultList<T> searchResult = new();

    foreach (var group in bestDistances
      .GroupBy(x => x.Value)
      .OrderBy(x => x.Key))
    {
      searchResult.Items.Add(
        group.Key,
        new IndexList<T>(group.Select(x => x.Key)));
    }

    return searchResult;
  }

  /// <summary>
  /// Проверяет корректность параметров поискового запроса.
  /// </summary>
  /// <param name="request">Параметры поиска.</param>
  /// <param name="error">Описание ошибки, если проверка не пройдена.</param>
  /// <returns>
  /// <see langword="true"/>, если параметры корректны;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool TryValidateRequest(SearchRequest? request, out SearchError? error)
  {
    if (request is null)
    {
      error = null;
      return true;
    }

    if (request.PrecisionSearch is < 0 or > 100)
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Точность поиска должна быть в диапазоне от 0 до 100.");
      return false;
    }

    if (request.AcceptableCountMisprint < 0)
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Допустимое количество опечаток не может быть отрицательным.");
      return false;
    }

    if (request.MatchMode == QueryMatchMode.SoftAllTerms)
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Режим SoftAllTerms пока не поддерживается.");
      return false;
    }

    error = null;
    return true;
  }

  /// <summary>
  /// Вычисляет допустимую дистанцию поиска по длине слова и точности.
  /// </summary>
  /// <param name="length">Длина искомого слова.</param>
  /// <param name="percent">Точность поиска в процентах.</param>
  /// <returns>Допустимая дистанция.</returns>
  private static int CalculateDistance(int length, int percent) =>
    length > 1 ? length - length * percent / 100 : 0;

  /// <summary>
  /// Определяет, относится ли исключение к критическим,
  /// которые не нужно скрывать за результатом.
  /// </summary>
  /// <param name="exception">Проверяемое исключение.</param>
  /// <returns>
  /// <see langword="true"/>, если исключение критическое;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool IsCriticalException(Exception exception) =>
    exception is OutOfMemoryException
    or StackOverflowException
    or AccessViolationException
    or AppDomainUnloadedException
    or BadImageFormatException
    or CannotUnloadAppDomainException;
}