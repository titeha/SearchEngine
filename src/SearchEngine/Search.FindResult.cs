using ResultType;

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
      return Result.Failure<SearchResultList<T>, SearchError>(validationError!);

    try
    {
      DisassemblyString(searchString);

      if (_searchList.Count == 0)
        return Result.Failure<SearchResultList<T>, SearchError>(
                  new SearchError(
                    SearchErrorCode.QueryHasNoSearchableTerms,
                    "Поисковый запрос не содержит пригодных для поиска слов."));

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
        QueryMatchMode.SoftAllTerms => ExecuteSoftAllTermsSearch(searchItems),
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
    Dictionary<T, int>? commonDistances = null;

    foreach (string item in searchItems)
    {
      Dictionary<T, int> itemDistances = ExtractBestDistances(ExecuteSingleItemSearch(item));

      if (commonDistances is null)
      {
        commonDistances = itemDistances;
        continue;
      }

      foreach (T index in commonDistances.Keys.ToArray())
        if (itemDistances.TryGetValue(index, out int itemDistance))
          commonDistances[index] += itemDistance;
        else
          commonDistances.Remove(index);

      if (commonDistances.Count == 0)
        break;
    }

    return BuildSearchResult(commonDistances ?? new Dictionary<T, int>());
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
      foreach (var pair in ExtractBestDistances(ExecuteSingleItemSearch(item)))
        if (!bestDistances.TryGetValue(pair.Key, out int currentDistance) ||
                    pair.Value < currentDistance)
          bestDistances[pair.Key] = pair.Value;

    return BuildSearchResult(bestDistances);
  }

  /// <summary>
  /// Выполняет поиск в мягком режиме:
  /// полные совпадения выше частичных, а внутри группы
  /// результаты ранжируются по суммарной дистанции.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <returns>Результат поиска.</returns>
  private SearchResultList<T> ExecuteSoftAllTermsSearch(IEnumerable<string> searchItems)
  {
    List<string> searchTerms = [.. searchItems];
    Dictionary<T, SearchRank> ranks = new();

    foreach (string item in searchTerms)
      foreach (var pair in ExtractBestDistances(ExecuteSingleItemSearch(item)))
        if (ranks.TryGetValue(pair.Key, out SearchRank currentRank))
        {
          ranks[pair.Key] = currentRank with
          {
            MatchedTerms = currentRank.MatchedTerms + 1,
            TotalDistance = currentRank.TotalDistance + pair.Value
          };
        }
        else
          ranks[pair.Key] = new SearchRank(1, pair.Value);

    return BuildSoftAllTermsResult(ranks, searchTerms.Count);
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
  /// Извлекает для каждого идентификатора лучшую дистанцию из результата поиска.
  /// </summary>
  /// <param name="searchResult">Результат поиска по одному слову.</param>
  /// <returns>Набор идентификаторов и их минимальных дистанций.</returns>
  private static Dictionary<T, int> ExtractBestDistances(SearchResultList<T> searchResult)
  {
    Dictionary<T, int> distances = new();

    foreach (var bucket in searchResult.Items)
      foreach (T index in bucket.Value.Items)
        if (!distances.TryGetValue(index, out int currentDistance) ||
                    bucket.Key < currentDistance)
          distances[index] = bucket.Key;

    return distances;
  }

  /// <summary>
  /// Создаёт результат поиска из набора дистанций по найденным идентификаторам.
  /// </summary>
  /// <param name="distances">Набор идентификаторов и их дистанций.</param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildSearchResult(IReadOnlyDictionary<T, int> distances)
  {
    SearchResultList<T> searchResult = new();

    foreach (var group in distances
      .GroupBy(x => x.Value)
      .OrderBy(x => x.Key))
      searchResult.Items.Add(
              group.Key,
              new IndexList<T>(group.Select(x => x.Key)));

    return searchResult;
  }

  /// <summary>
  /// Создаёт результат поиска для мягкого режима.
  /// Значение корзины представляет составной ранг:
  /// сначала учитывается количество совпавших слов,
  /// затем суммарная дистанция.
  /// </summary>
  /// <param name="ranks">Набор рангов по найденным идентификаторам.</param>
  /// <param name="termsCount">Количество слов в запросе.</param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildSoftAllTermsResult(
    IReadOnlyDictionary<T, SearchRank> ranks,
    int termsCount)
  {
    if (ranks.Count == 0 || termsCount == 0)
      return new SearchResultList<T>();

    int maxObservedDistance = ranks.Max(x => x.Value.TotalDistance);
    int missingTermPenalty = maxObservedDistance + 1;

    Dictionary<T, int> scores = new();

    foreach (var pair in ranks)
    {
      int missingTerms = termsCount - pair.Value.MatchedTerms;
      scores[pair.Key] = pair.Value.TotalDistance + missingTerms * missingTermPenalty;
    }

    return BuildSearchResult(scores);
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

    if (!Enum.IsDefined(request.MatchMode))
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Режим объединения слов запроса имеет недопустимое значение.");
      return false;
    }

    if (request.SearchType.HasValue &&
        !Enum.IsDefined(request.SearchType.Value))
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Тип поискового механизма имеет недопустимое значение.");
      return false;
    }

    if (request.SearchLocation.HasValue &&
        !Enum.IsDefined(request.SearchLocation.Value))
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Место поиска имеет недопустимое значение.");
      return false;
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

  /// <summary>
  /// Внутренний ранг результата для мягкого режима поиска.
  /// </summary>
  /// <param name="MatchedTerms">Количество совпавших слов.</param>
  /// <param name="TotalDistance">Суммарная дистанция.</param>
  private readonly record struct SearchRank(int MatchedTerms, int TotalDistance);
}