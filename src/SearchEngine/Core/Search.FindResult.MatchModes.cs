namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
public partial class Search<T> where T : struct
{
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
}
