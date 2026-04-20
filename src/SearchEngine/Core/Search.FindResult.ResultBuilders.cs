using SearchEngine.Models;

namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
public partial class Search<T> where T : struct
{
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
  /// Создаёт результирующий список индексов с защитной копией данных.
  /// </summary>
  /// <param name="indexes">Исходный отсортированный список индексов.</param>
  /// <returns>Список индексов для выдачи наружу.</returns>
  private static IndexList<T> CreateResultIndexList(
    IReadOnlyList<T> indexes)
  {
    return new IndexList<T>(
      [.. indexes],
      sort: false);
  }
}
