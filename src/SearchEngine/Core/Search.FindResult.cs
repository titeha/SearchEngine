using ResultType;

using SearchEngine.Models;

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
  /// Выполняет поиск отдельно для каждого слова запроса.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <returns>Результаты поиска по отдельным словам.</returns>
  private SearchResultList<T>[] ExecuteTermSearches(
    IReadOnlyList<string> searchItems,
    SearchExecutionOptions executionOptions)
  {
    SearchResultList<T>[] termResults = new SearchResultList<T>[searchItems.Count];

    for (int i = 0; i < searchItems.Count; i++)
      termResults[i] = ExecuteSingleItemSearch(searchItems[i], executionOptions);

    return termResults;
  }

  /// <summary>
  /// Пытается построить результат поиска быстрым путём,
  /// если все результаты по словам находятся только в нулевой дистанции.
  /// </summary>
  /// <param name="termResults">Результаты поиска по отдельным словам.</param>
  /// <param name="matchMode">Режим объединения слов запроса.</param>
  /// <param name="searchResult">Построенный результат поиска.</param>
  /// <returns>
  /// <see langword="true"/>, если результат был построен быстрым путём.
  /// </returns>
  private static bool TryBuildZeroDistanceSearchResult(
    IReadOnlyList<SearchResultList<T>> termResults,
    QueryMatchMode matchMode,
    out SearchResultList<T>? searchResult)
  {
    searchResult = null;

    if (!ContainsOnlyZeroDistanceResults(termResults))
      return false;

    searchResult = matchMode switch
    {
      QueryMatchMode.AllTerms => BuildZeroDistanceAllTermsResult(termResults),
      QueryMatchMode.AnyTerm => BuildZeroDistanceAnyTermResult(termResults),
      QueryMatchMode.SoftAllTerms => BuildZeroDistanceSoftAllTermsResult(termResults),

      _ => throw new InvalidOperationException(
        $"Режим {matchMode} не поддерживается текущей реализацией поиска.")
    };

    return true;
  }

  /// <summary>
  /// Проверяет, что каждый результат содержит только корзину нулевой дистанции
  /// либо не содержит результатов вообще.
  /// </summary>
  /// <param name="termResults">Результаты поиска по отдельным словам.</param>
  /// <returns>
  /// <see langword="true"/>, если все результаты подходят для быстрого объединения.
  /// </returns>
  private static bool ContainsOnlyZeroDistanceResults(
    IReadOnlyList<SearchResultList<T>> termResults)
  {
    foreach (SearchResultList<T> termResult in termResults)
    {
      if (termResult.Items.Count == 0)
        continue;

      if (termResult.Items.Count != 1)
        return false;

      if (!termResult.Items.ContainsKey(0))
        return false;
    }

    return true;
  }

  /// <summary>
  /// Строит результат <see cref="QueryMatchMode.AllTerms"/> для точных совпадений.
  /// </summary>
  /// <param name="termResults">Результаты поиска по отдельным словам.</param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildZeroDistanceAllTermsResult(
    IReadOnlyList<SearchResultList<T>> termResults)
  {
    SearchResultList<T> result = new();

    if (termResults.Count == 0)
      return result;

    if (!TryGetZeroDistanceIndexes(termResults[0], out IndexList<T>? firstIndexes))
      return result;

    List<T> intersection = [.. firstIndexes!.Items];

    for (int i = 1; i < termResults.Count; i++)
    {
      if (!TryGetZeroDistanceIndexes(termResults[i], out IndexList<T>? indexes))
        return new SearchResultList<T>();

      intersection = IntersectSortedLists(intersection, indexes!.InternalItems);

      if (intersection.Count == 0)
        return new SearchResultList<T>();
    }

    result.Items.Add(0, new IndexList<T>(intersection, sort: false));

    return result;
  }

  /// <summary>
  /// Строит результат <see cref="QueryMatchMode.AnyTerm"/> для точных совпадений.
  /// </summary>
  /// <param name="termResults">Результаты поиска по отдельным словам.</param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildZeroDistanceAnyTermResult(
    IReadOnlyList<SearchResultList<T>> termResults)
  {
    SearchResultList<T> result = new();

    List<T>? union = null;

    foreach (SearchResultList<T> termResult in termResults)
    {
      if (!TryGetZeroDistanceIndexes(termResult, out IndexList<T>? indexes))
        continue;

      union = union is null
        ? [.. indexes!.Items]
        : UnionSortedLists(union, indexes!.InternalItems);
    }

    if (union is { Count: > 0 })
      result.Items.Add(0, new IndexList<T>(union, sort: false));

    return result;
  }

  /// <summary>
  /// Строит результат <see cref="QueryMatchMode.SoftAllTerms"/> для точных совпадений.
  /// </summary>
  /// <param name="termResults">Результаты поиска по отдельным словам.</param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildZeroDistanceSoftAllTermsResult(
    IReadOnlyList<SearchResultList<T>> termResults)
  {
    SearchResultList<T> result = new();

    if (termResults.Count == 0)
      return result;

    List<IReadOnlyList<T>> indexLists = [];

    foreach (SearchResultList<T> termResult in termResults)
    {
      if (TryGetZeroDistanceIndexes(termResult, out IndexList<T>? indexes))
        indexLists.Add(indexes!.InternalItems);
    }

    if (indexLists.Count == 0)
      return result;

    int[] positions = new int[indexLists.Count];
    List<T>?[] buckets = new List<T>[termResults.Count];

    Comparer<T> comparer = Comparer<T>.Default;

    while (TryGetMinimalCurrentValue(indexLists, positions, comparer, out T currentValue))
    {
      int matchedTerms = 0;

      for (int i = 0; i < indexLists.Count; i++)
      {
        IReadOnlyList<T> indexes = indexLists[i];

        if (positions[i] >= indexes.Count)
          continue;

        if (comparer.Compare(indexes[positions[i]], currentValue) != 0)
          continue;

        matchedTerms++;

        do
        {
          positions[i]++;
        }
        while (
          positions[i] < indexes.Count &&
          comparer.Compare(indexes[positions[i]], currentValue) == 0);
      }

      int missingTerms = termResults.Count - matchedTerms;

      buckets[missingTerms] ??= [];
      buckets[missingTerms]!.Add(currentValue);
    }

    for (int score = 0; score < buckets.Length; score++)
    {
      List<T>? bucket = buckets[score];

      if (bucket is { Count: > 0 })
        result.Items.Add(score, new IndexList<T>(bucket, sort: false));
    }

    return result;
  }

  /// <summary>
  /// Получает список идентификаторов из корзины нулевой дистанции.
  /// </summary>
  /// <param name="searchResult">Результат поиска.</param>
  /// <param name="indexes">Список идентификаторов.</param>
  /// <returns>
  /// <see langword="true"/>, если корзина нулевой дистанции существует и содержит элементы.
  /// </returns>
  private static bool TryGetZeroDistanceIndexes(
    SearchResultList<T> searchResult,
    out IndexList<T>? indexes)
  {
    indexes = null;

    if (!searchResult.Items.TryGetValue(0, out IndexList<T>? zeroDistanceIndexes))
      return false;

    if (zeroDistanceIndexes.Count == 0)
      return false;

    indexes = zeroDistanceIndexes;

    return true;
  }

  /// <summary>
  /// Выполняет поиск в режиме строгого совпадения всех слов запроса.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <returns>Результат поиска.</returns>
  private SearchResultList<T> ExecuteAllTermsSearch(IEnumerable<string> searchItems, SearchExecutionOptions executionOptions)
  {
    Dictionary<T, int>? commonDistances = null;

    foreach (string item in searchItems)
    {
      Dictionary<T, int> itemDistances = ExtractBestDistances(ExecuteSingleItemSearch(item, executionOptions));

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

    return BuildSearchResult(commonDistances ?? []);
  }

  /// <summary>
  /// Выполняет поиск в режиме совпадения по любому слову запроса.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <returns>Результат поиска.</returns>
  private SearchResultList<T> ExecuteAnyTermSearch(IEnumerable<string> searchItems, SearchExecutionOptions executionOptions)
  {
    Dictionary<T, int> bestDistances = new();

    foreach (string item in searchItems)
      foreach (var pair in ExtractBestDistances(ExecuteSingleItemSearch(item, executionOptions)))
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
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <returns>Результат поиска.</returns>
  private SearchResultList<T> ExecuteSoftAllTermsSearch(IEnumerable<string> searchItems, SearchExecutionOptions executionOptions)
  {
    List<string> searchTerms = [.. searchItems];
    Dictionary<T, SearchRank> ranks = new();

    foreach (string item in searchTerms)
      foreach (var pair in ExtractBestDistances(ExecuteSingleItemSearch(item, executionOptions)))
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
  /// Эффективные параметры выполнения конкретного поискового запроса.
  /// </summary>
  /// <param name="SearchType">Тип поискового механизма.</param>
  /// <param name="SearchLocation">Место поиска.</param>
  /// <param name="PrecisionSearch">Точность поиска в процентах.</param>
  /// <param name="AcceptableCountMisprint">Допустимое количество опечаток.</param>
  private readonly record struct SearchExecutionOptions(
    SearchType SearchType,
    SearchLocation SearchLocation,
    int PrecisionSearch,
    int AcceptableCountMisprint);

  /// <summary>
  /// Внутренний ранг результата для мягкого режима поиска.
  /// </summary>
  /// <param name="MatchedTerms">Количество совпавших слов.</param>
  /// <param name="TotalDistance">Суммарная дистанция.</param>
  private readonly record struct SearchRank(int MatchedTerms, int TotalDistance);
}