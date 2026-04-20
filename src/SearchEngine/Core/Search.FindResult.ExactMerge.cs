using SearchEngine.Models;

namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
public partial class Search<T> where T : struct
{
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

  private static SearchResultList<T> BuildExactZeroDistanceSingleTermResult(
  IReadOnlyList<T> indexes)
  {
    SearchResultList<T> result = new();

    if (indexes.Count == 0)
      return result;

    result.Items.Add(
      0,
      CreateResultIndexList(indexes));

    return result;
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

    if (TryGetEquivalentAllTermsSource(termIndexes, out IReadOnlyList<T>? equivalentIndexes))
    {
      result.Items.Add(
        0,
        CreateResultIndexList(equivalentIndexes!));

      return result;
    }

    List<T> intersection = IntersectAllSortedLists(termIndexes);

    if (intersection.Count == 0)
      return result;

    result.Items.Add(
      0,
      new IndexList<T>(intersection, sort: false));

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

    if (TryGetEquivalentAnyTermSource(termIndexes, out IReadOnlyList<T>? equivalentIndexes))
    {
      result.Items.Add(
        0,
        CreateResultIndexList(equivalentIndexes!));

      return result;
    }

    List<IReadOnlyList<T>> nonEmptyIndexes = [];
    int capacity = 0;

    foreach (IReadOnlyList<T> indexes in termIndexes)
    {
      if (indexes.Count == 0)
        continue;

      nonEmptyIndexes.Add(indexes);
      capacity += indexes.Count;
    }

    if (nonEmptyIndexes.Count == 0)
      return result;

    List<T> union = UnionAllSortedLists(
      nonEmptyIndexes,
      capacity);

    if (union.Count > 0)
    {
      result.Items.Add(
        0,
        new IndexList<T>(union, sort: false));
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
    if (TryBuildEquivalentSoftAllTermsResult(termIndexes, out SearchResultList<T> equivalentResult))
      return equivalentResult;

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
  /// Пытается получить общий список, если все списки идентификаторов непустые и одинаковые.
  /// </summary>
  /// <param name="termIndexes">Списки идентификаторов по словам запроса.</param>
  /// <param name="indexes">Общий список идентификаторов.</param>
  /// <returns>
  /// <see langword="true"/>, если все списки непустые и одинаковые.
  /// </returns>
  private static bool TryGetEquivalentAllTermsSource(
    IReadOnlyList<T>[] termIndexes,
    out IReadOnlyList<T>? indexes)
  {
    indexes = null;

    if (termIndexes.Length == 0)
      return false;

    IReadOnlyList<T> first = termIndexes[0];

    if (first.Count == 0)
      return false;

    for (int i = 1; i < termIndexes.Length; i++)
    {
      IReadOnlyList<T> current = termIndexes[i];

      if (current.Count == 0)
        return false;

      if (!AreSortedListsEqual(first, current))
        return false;
    }

    indexes = first;

    return true;
  }

  /// <summary>
  /// Пытается получить единственный список, если все непустые списки идентификаторов одинаковые.
  /// </summary>
  /// <param name="termIndexes">Списки идентификаторов по словам запроса.</param>
  /// <param name="indexes">Единственный список идентификаторов.</param>
  /// <returns>
  /// <see langword="true"/>, если все непустые списки одинаковые.
  /// </returns>
  private static bool TryGetEquivalentAnyTermSource(
    IReadOnlyList<T>[] termIndexes,
    out IReadOnlyList<T>? indexes)
  {
    indexes = null;

    foreach (IReadOnlyList<T> current in termIndexes)
    {
      if (current.Count == 0)
        continue;

      if (indexes is null)
      {
        indexes = current;
        continue;
      }

      if (!AreSortedListsEqual(indexes, current))
        return false;
    }

    return indexes is not null;
  }

  /// <summary>
  /// Пытается построить результат <see cref="QueryMatchMode.SoftAllTerms"/>,
  /// если все непустые списки идентификаторов одинаковые.
  /// </summary>
  /// <param name="termIndexes">Списки идентификаторов по словам запроса.</param>
  /// <param name="searchResult">Результат поиска.</param>
  /// <returns>
  /// <see langword="true"/>, если результат был построен быстрым путём.
  /// </returns>
  private static bool TryBuildEquivalentSoftAllTermsResult(
    IReadOnlyList<T>[] termIndexes,
    out SearchResultList<T> searchResult)
  {
    searchResult = new();

    IReadOnlyList<T>? commonIndexes = null;
    int matchedTerms = 0;

    foreach (IReadOnlyList<T> current in termIndexes)
    {
      if (current.Count == 0)
        continue;

      matchedTerms++;

      if (commonIndexes is null)
      {
        commonIndexes = current;
        continue;
      }

      if (!AreSortedListsEqual(commonIndexes, current))
        return false;
    }

    if (commonIndexes is null)
      return true;

    int missingTerms = termIndexes.Length - matchedTerms;

    searchResult.Items.Add(
      missingTerms,
      CreateResultIndexList(commonIndexes));

    return true;
  }
}
