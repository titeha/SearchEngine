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
  /// Выполняет поиск по подготовленному набору слов.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <param name="matchMode">Режим объединения слов запроса.</param>
  /// <returns>Результат поиска.</returns>
  private SearchResultList<T> ExecuteSearch(
    IReadOnlyList<string> searchItems,
    SearchExecutionOptions executionOptions,
    QueryMatchMode matchMode)
  {
    if (searchItems.Count == 1)
      return ExecuteSingleTermSearch(searchItems[0], executionOptions);

    if (!IsPhoneticSearch && executionOptions.SearchType == SearchType.ExactSearch)
    {
      IReadOnlyList<T>[] termIndexes = ExecuteExactZeroDistanceTermSearches(
        searchItems,
        executionOptions);

      return BuildExactZeroDistanceSearchResult(
        termIndexes,
        matchMode);
    }

    return matchMode switch
    {
      QueryMatchMode.AllTerms => ExecuteAllTermsSearch(searchItems, executionOptions),
      QueryMatchMode.AnyTerm => ExecuteAnyTermSearch(searchItems, executionOptions),
      QueryMatchMode.SoftAllTerms => ExecuteSoftAllTermsSearch(searchItems, executionOptions),

      _ => throw new InvalidOperationException(
        $"Режим {matchMode} не поддерживается текущей реализацией поиска.")
    };
  }

  /// <summary>
  /// Выполняет точный поиск отдельно для каждого слова запроса
  /// и возвращает только списки идентификаторов нулевой дистанции.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <returns>Списки идентификаторов по каждому слову запроса.</returns>
  private IReadOnlyList<T>[] ExecuteExactZeroDistanceTermSearches(
    IReadOnlyList<string> searchItems,
    SearchExecutionOptions executionOptions)
  {
    IReadOnlyList<T>[] termIndexes = new IReadOnlyList<T>[searchItems.Count];

    for (int i = 0; i < searchItems.Count; i++)
    {
      termIndexes[i] = ExecuteExactZeroDistanceItemSearch(
        searchItems[i],
        executionOptions.SearchLocation);
    }

    return termIndexes;
  }

  /// <summary>
  /// Выполняет точный поиск по одному слову и возвращает список найденных идентификаторов
  /// без создания промежуточного <see cref="SearchResultList{T}"/>.
  /// </summary>
  /// <param name="searchValue">Искомое слово.</param>
  /// <param name="searchLocation">Место поиска внутри слова.</param>
  /// <returns>Отсортированный список найденных идентификаторов.</returns>
  private IReadOnlyList<T> ExecuteExactZeroDistanceItemSearch(
    string searchValue,
    SearchLocation searchLocation)
  {
    if (_searchIndex is null || _searchIndex.Count == 0)
      return [];

    bool origin = SearchLocation.BeginWord == searchLocation;

    IReadOnlyList<T>? singleMatch = null;
    List<T>? union = null;

    foreach (var pair in _searchIndex)
    {
      if (!IsExactSearchKeyMatch(pair.Key, searchValue, origin))
        continue;

      IReadOnlyList<T> indexes = pair.Value.InternalItems;

      if (indexes.Count == 0)
        continue;

      if (singleMatch is null && union is null)
      {
        singleMatch = indexes;
        continue;
      }

      union = union is null
        ? UnionSortedLists(singleMatch!, indexes)
        : UnionSortedLists(union, indexes);

      singleMatch = null;
    }

    return (IReadOnlyList<T>?)union ?? singleMatch ?? [];
  }

  /// <summary>
  /// Проверяет, подходит ли ключ индекса под точный поиск.
  /// </summary>
  /// <param name="indexKey">Ключ поискового индекса.</param>
  /// <param name="searchValue">Искомое слово.</param>
  /// <param name="origin">
  /// <see langword="true"/>, если совпадение должно быть в начале слова.
  /// </param>
  /// <returns>
  /// <see langword="true"/>, если ключ подходит под поисковый запрос.
  /// </returns>
  private static bool IsExactSearchKeyMatch(
    string indexKey,
    string searchValue,
    bool origin)
  {
    if (indexKey.Length < searchValue.Length)
      return false;

    int position = indexKey.IndexOf(
      searchValue,
      StringComparison.OrdinalIgnoreCase);

    return origin
      ? position == 0
      : position >= 0;
  }

  /// <summary>
  /// Строит результат точного поиска по спискам идентификаторов отдельных слов.
  /// </summary>
  /// <param name="termIndexes">Списки идентификаторов по словам запроса.</param>
  /// <param name="matchMode">Режим объединения слов запроса.</param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildExactZeroDistanceSearchResult(
    IReadOnlyList<T>[] termIndexes,
    QueryMatchMode matchMode)
  {
    return matchMode switch
    {
      QueryMatchMode.AllTerms => BuildExactZeroDistanceAllTermsResult(termIndexes),
      QueryMatchMode.AnyTerm => BuildExactZeroDistanceAnyTermResult(termIndexes),
      QueryMatchMode.SoftAllTerms => BuildExactZeroDistanceSoftAllTermsResult(termIndexes),

      _ => throw new InvalidOperationException(
        $"Режим {matchMode} не поддерживается текущей реализацией поиска.")
    };
  }

  /// <summary>
  /// Строит результат <see cref="QueryMatchMode.AllTerms"/> для точных совпадений
  /// по готовым спискам идентификаторов.
  /// </summary>
  /// <param name="termIndexes">Списки идентификаторов по словам запроса.</param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildExactZeroDistanceAllTermsResult(
    IReadOnlyList<T>[] termIndexes)
  {
    SearchResultList<T> result = new();

    if (termIndexes.Length == 0)
      return result;

    for (int i = 0; i < termIndexes.Length; i++)
      if (termIndexes[i].Count == 0)
        return result;

    int smallestIndex = FindSmallestIndexList(termIndexes);

    IReadOnlyList<T> current = termIndexes[smallestIndex];

    for (int i = 0; i < termIndexes.Length; i++)
    {
      if (i == smallestIndex)
        continue;

      current = IntersectSortedLists(
        current,
        termIndexes[i]);

      if (current.Count == 0)
        return new SearchResultList<T>();
    }

    result.Items.Add(
      0,
      CreateResultIndexList(current));

    return result;
  }

  /// <summary>
  /// Строит результат <see cref="QueryMatchMode.AnyTerm"/> для точных совпадений
  /// по готовым спискам идентификаторов.
  /// </summary>
  /// <param name="termIndexes">Списки идентификаторов по словам запроса.</param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildExactZeroDistanceAnyTermResult(
    IReadOnlyList<T>[] termIndexes)
  {
    SearchResultList<T> result = new();

    IReadOnlyList<T>? singleSource = null;
    List<T>? union = null;

    foreach (IReadOnlyList<T> indexes in termIndexes)
    {
      if (indexes.Count == 0)
        continue;

      if (singleSource is null && union is null)
      {
        singleSource = indexes;
        continue;
      }

      union = union is null
        ? UnionSortedLists(singleSource!, indexes)
        : UnionSortedLists(union, indexes);

      singleSource = null;
    }

    if (union is not null)
    {
      result.Items.Add(
        0,
        new IndexList<T>(union, sort: false));

      return result;
    }

    if (singleSource is not null)
    {
      result.Items.Add(
        0,
        CreateResultIndexList(singleSource));
    }

    return result;
  }

  /// <summary>
  /// Строит результат <see cref="QueryMatchMode.SoftAllTerms"/> для точных совпадений
  /// по готовым спискам идентификаторов.
  /// </summary>
  /// <param name="termIndexes">Списки идентификаторов по словам запроса.</param>
  /// <returns>Результат поиска.</returns>
  private static SearchResultList<T> BuildExactZeroDistanceSoftAllTermsResult(
    IReadOnlyList<T>[] termIndexes)
  {
    SearchResultList<T> result = new();

    if (termIndexes.Length == 0)
      return result;

    List<IReadOnlyList<T>> nonEmptyIndexes = [];

    foreach (IReadOnlyList<T> indexes in termIndexes)
      if (indexes.Count > 0)
        nonEmptyIndexes.Add(indexes);

    if (nonEmptyIndexes.Count == 0)
      return result;

    int[] positions = new int[nonEmptyIndexes.Count];
    List<T>?[] buckets = new List<T>[termIndexes.Length];

    Comparer<T> comparer = Comparer<T>.Default;

    while (TryGetMinimalCurrentValue(
      nonEmptyIndexes,
      positions,
      comparer,
      out T currentValue))
    {
      int matchedTerms = 0;

      for (int i = 0; i < nonEmptyIndexes.Count; i++)
      {
        IReadOnlyList<T> indexes = nonEmptyIndexes[i];

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

      int missingTerms = termIndexes.Length - matchedTerms;

      buckets[missingTerms] ??= [];
      buckets[missingTerms]!.Add(currentValue);
    }

    for (int score = 0; score < buckets.Length; score++)
    {
      List<T>? bucket = buckets[score];

      if (bucket is { Count: > 0 })
        result.Items.Add(
          score,
          new IndexList<T>(bucket, sort: false));
    }

    return result;
  }

  /// <summary>
  /// Находит самый короткий список идентификаторов.
  /// </summary>
  /// <param name="indexLists">Списки идентификаторов.</param>
  /// <returns>Индекс самого короткого списка.</returns>
  private static int FindSmallestIndexList(
    IReadOnlyList<T>[] indexLists)
  {
    int smallestIndex = 0;
    int smallestCount = indexLists[0].Count;

    for (int i = 1; i < indexLists.Length; i++)
    {
      if (indexLists[i].Count >= smallestCount)
        continue;

      smallestIndex = i;
      smallestCount = indexLists[i].Count;
    }

    return smallestIndex;
  }

  /// <summary>
  /// Создаёт результирующий список индексов с защитной копией данных.
  /// </summary>
  /// <param name="indexes">Исходный отсортированный список индексов.</param>
  /// <returns>Список индексов для выдачи наружу.</returns>
  private static IndexList<T> CreateResultIndexList(
    IReadOnlyList<T> indexes)
  {
    return new IndexList<T>(
      new List<T>(indexes),
      sort: false);
  }

  /// <summary>
  /// Строит пересечение двух отсортированных списков.
  /// </summary>
  /// <param name="left">Первый список.</param>
  /// <param name="right">Второй список.</param>
  /// <returns>Отсортированный список общих элементов.</returns>
  private static List<T> IntersectSortedLists(
    IReadOnlyList<T> left,
    IReadOnlyList<T> right)
  {
    List<T> result = new(Math.Min(left.Count, right.Count));

    Comparer<T> comparer = Comparer<T>.Default;

    int leftIndex = 0;
    int rightIndex = 0;

    while (leftIndex < left.Count && rightIndex < right.Count)
    {
      T leftValue = left[leftIndex];
      T rightValue = right[rightIndex];

      int comparison = comparer.Compare(leftValue, rightValue);

      if (comparison == 0)
      {
        AddIfDifferentFromLast(result, leftValue, comparer);

        leftIndex++;
        rightIndex++;

        continue;
      }

      if (comparison < 0)
        leftIndex++;
      else
        rightIndex++;
    }

    return result;
  }

  /// <summary>
  /// Строит объединение двух отсортированных списков без дублей.
  /// </summary>
  /// <param name="left">Первый список.</param>
  /// <param name="right">Второй список.</param>
  /// <returns>Отсортированный список уникальных элементов.</returns>
  private static List<T> UnionSortedLists(
    IReadOnlyList<T> left,
    IReadOnlyList<T> right)
  {
    List<T> result = new(left.Count + right.Count);

    Comparer<T> comparer = Comparer<T>.Default;

    int leftIndex = 0;
    int rightIndex = 0;

    while (leftIndex < left.Count && rightIndex < right.Count)
    {
      T leftValue = left[leftIndex];
      T rightValue = right[rightIndex];

      int comparison = comparer.Compare(leftValue, rightValue);

      if (comparison == 0)
      {
        AddIfDifferentFromLast(result, leftValue, comparer);

        leftIndex++;
        rightIndex++;

        continue;
      }

      if (comparison < 0)
      {
        AddIfDifferentFromLast(result, leftValue, comparer);
        leftIndex++;
      }
      else
      {
        AddIfDifferentFromLast(result, rightValue, comparer);
        rightIndex++;
      }
    }

    while (leftIndex < left.Count)
    {
      AddIfDifferentFromLast(result, left[leftIndex], comparer);
      leftIndex++;
    }

    while (rightIndex < right.Count)
    {
      AddIfDifferentFromLast(result, right[rightIndex], comparer);
      rightIndex++;
    }

    return result;
  }

  /// <summary>
  /// Добавляет значение в список, если оно отличается от последнего добавленного значения.
  /// </summary>
  /// <param name="items">Список значений.</param>
  /// <param name="value">Добавляемое значение.</param>
  /// <param name="comparer">Сравниватель значений.</param>
  private static void AddIfDifferentFromLast(
    List<T> items,
    T value,
    Comparer<T> comparer)
  {
    if (items.Count == 0)
    {
      items.Add(value);
      return;
    }

    if (comparer.Compare(items[^1], value) != 0)
      items.Add(value);
  }

  /// <summary>
  /// Получает минимальное текущее значение среди нескольких отсортированных списков.
  /// </summary>
  /// <param name="indexLists">Списки идентификаторов.</param>
  /// <param name="positions">Текущие позиции в списках.</param>
  /// <param name="comparer">Сравниватель значений.</param>
  /// <param name="value">Минимальное текущее значение.</param>
  /// <returns>
  /// <see langword="true"/>, если минимальное значение найдено.
  /// </returns>
  private static bool TryGetMinimalCurrentValue(
    IReadOnlyList<IReadOnlyList<T>> indexLists,
    IReadOnlyList<int> positions,
    Comparer<T> comparer,
    out T value)
  {
    value = default;

    bool hasValue = false;

    for (int i = 0; i < indexLists.Count; i++)
    {
      IReadOnlyList<T> indexes = indexLists[i];

      if (positions[i] >= indexes.Count)
        continue;

      T currentValue = indexes[positions[i]];

      if (!hasValue || comparer.Compare(currentValue, value) < 0)
      {
        value = currentValue;
        hasValue = true;
      }
    }

    return hasValue;
  }

  /// <summary>
  /// Выполняет поиск по одному слову запроса без маршрутизации через режимы объединения.
  /// </summary>
  /// <param name="item">Искомое слово.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <returns>Нормализованный результат поиска по одному слову.</returns>
  private SearchResultList<T> ExecuteSingleTermSearch(
    string item,
    SearchExecutionOptions executionOptions)
  {
    SearchResultList<T> searchResult = ExecuteSingleItemSearch(item, executionOptions);

    return BuildSingleTermSearchResult(searchResult);
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
  /// Создаёт результат поиска для запроса из одного слова.
  /// </summary>
  /// <param name="searchResult">Исходный результат поиска по одному слову.</param>
  /// <returns>
  /// Результат, в котором каждый идентификатор находится только в лучшей для него корзине дистанции.
  /// </returns>
  private static SearchResultList<T> BuildSingleTermSearchResult(
    SearchResultList<T> searchResult)
  {
    SearchResultList<T> result = new();

    if (searchResult.Items.Count == 0)
      return result;

    if (searchResult.Items.Count == 1)
    {
      int distance = searchResult.Items.Keys[0];
      IndexList<T> indexes = searchResult.Items.Values[0];

      if (indexes.Count > 0)
        result.Items.Add(distance, new IndexList<T>(indexes.Items));

      return result;
    }

    HashSet<T> usedIndexes = [];

    foreach (var bucket in searchResult.Items)
    {
      List<T>? bucketIndexes = null;

      foreach (T index in bucket.Value.Items)
      {
        if (!usedIndexes.Add(index))
          continue;

        bucketIndexes ??= [];
        bucketIndexes.Add(index);
      }

      if (bucketIndexes is { Count: > 0 })
        result.Items.Add(bucket.Key, new IndexList<T>(bucketIndexes, sort: true));
    }

    return result;
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
  /// Выполняет поиск по одному слову.
  /// </summary>
  /// <param name="item">Искомое слово.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <returns>Результат поиска по одному слову.</returns>
  private SearchResultList<T> ExecuteSingleItemSearch(string item, SearchExecutionOptions executionOptions)
  {
    if (IsPhoneticSearch)
      return PhoneticFind(item);

    if (item.Length == 2 || executionOptions.SearchType == SearchType.ExactSearch)
      return ExactSearch(item, executionOptions.SearchLocation);

    int distance = executionOptions.AcceptableCountMisprint >= 0
      ? executionOptions.AcceptableCountMisprint
      : CalculateDistance(item.Length, executionOptions.PrecisionSearch);

    return FuzzySearch(item, distance, executionOptions.SearchLocation);
  }

  /// <summary>
  /// Извлекает для каждого идентификатора лучшую дистанцию из результата поиска.
  /// </summary>
  /// <param name="searchResult">Результат поиска по одному слову.</param>
  /// <returns>Набор идентификаторов и их минимальных дистанций.</returns>
  private static Dictionary<T, int> ExtractBestDistances(SearchResultList<T> searchResult)
  {
    int capacity = 0;

    foreach (var bucket in searchResult.Items)
      capacity += bucket.Value.Count;

    Dictionary<T, int> distances = new(capacity);

    foreach (var bucket in searchResult.Items)
    {
      int distance = bucket.Key;

      foreach (T index in bucket.Value.Items)
        if (!distances.TryGetValue(index, out int currentDistance) || distance < currentDistance)
          distances[index] = distance;
    }

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

    if (distances.Count == 0)
      return searchResult;

    Dictionary<int, int> bucketSizes = new();

    foreach (var pair in distances)
    {
      if (bucketSizes.TryGetValue(pair.Value, out int count))
        bucketSizes[pair.Value] = count + 1;
      else
        bucketSizes.Add(pair.Value, 1);
    }

    SortedList<int, List<T>> buckets = new(bucketSizes.Count);

    foreach (var pair in bucketSizes)
      buckets.Add(pair.Key, new List<T>(pair.Value));

    foreach (var pair in distances)
      buckets[pair.Value].Add(pair.Key);

    foreach (var pair in buckets)
      searchResult.Items.Add(pair.Key, new IndexList<T>(pair.Value, sort: true));

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

    int maxObservedDistance = 0;

    foreach (var pair in ranks)
      if (pair.Value.TotalDistance > maxObservedDistance)
        maxObservedDistance = pair.Value.TotalDistance;

    int missingTermPenalty = maxObservedDistance + 1;

    Dictionary<T, int> scores = new(ranks.Count);

    foreach (var pair in ranks)
    {
      int missingTerms = termsCount - pair.Value.MatchedTerms;
      scores[pair.Key] = pair.Value.TotalDistance + missingTerms * missingTermPenalty;
    }

    return BuildSearchResult(scores);
  }

  /// <summary>
  /// Формирует эффективные параметры выполнения поиска
  /// с учётом настроек экземпляра и параметров запроса.
  /// </summary>
  /// <param name="request">Параметры поиска.</param>
  /// <returns>Параметры выполнения конкретного запроса.</returns>
  private SearchExecutionOptions CreateExecutionOptions(SearchRequest? request) => new(
    request?.SearchType ?? SearchType,
    request?.SearchLocation ?? SearchLocation,
    request?.PrecisionSearch ?? PrecisionSearch,
    request?.AcceptableCountMisprint ?? AcceptableCountMisprint);

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