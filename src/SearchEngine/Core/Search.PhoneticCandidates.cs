using StringFunctions;

namespace SearchEngine;

public partial class Search<T> where T : struct
{
  /// <summary>
  /// Минимальная длина фонетического ключа для префиксного поиска.
  /// </summary>
  private const int _minPhoneticPrefixLength = 7;

  /// <summary>
  /// Перестраивает индекс фонетических кандидатов для поиска на расстоянии одной правки.
  /// </summary>
  private void RebuildPhoneticCandidateIndex()
  {
    if (!IsPhoneticSearch || _searchIndex is null || _searchIndex.Count == 0)
    {
      _phoneticCandidateIndex = null;
      return;
    }

    Dictionary<string, List<string>> candidateIndex = new(StringComparer.Ordinal);

    foreach (string key in _searchIndex.Keys)
    {
      if (key.IsNullOrEmpty())
        continue;

      AddPhoneticCandidate(candidateIndex, key, key);

      for (int i = 0; i < key.Length; i++)
      {
        string deletionKey = CreateDeletionKey(key, i);

        AddPhoneticCandidate(candidateIndex, deletionKey, key);
      }
    }

    _phoneticCandidateIndex = candidateIndex;
  }

  /// <summary>
  /// Возвращает фонетические ключи-кандидаты для заданного ключа запроса.
  /// </summary>
  /// <param name="searchValue">Фонетический ключ запроса.</param>
  /// <returns>Ключи-кандидаты из поискового индекса.</returns>
  private IEnumerable<string> EnumeratePhoneticCandidateKeys(string searchValue)
  {
    if (_searchIndex is null || _searchIndex.Count == 0)
      yield break;

    HashSet<string> emittedKeys = new(StringComparer.Ordinal);

    foreach (string key in EnumeratePhoneticPrefixKeys(searchValue))
      if (emittedKeys.Add(key))
        yield return key;

    if (_phoneticCandidateIndex is null)
    {
      foreach (string key in _searchIndex.Keys)
        if (emittedKeys.Add(key))
          yield return key;

      yield break;
    }

    foreach (string lookupKey in EnumerateLookupKeys(searchValue))
    {
      if (!_phoneticCandidateIndex.TryGetValue(lookupKey, out List<string>? candidates))
        continue;

      foreach (string candidate in candidates)
      {
        if (candidate.Length < searchValue.Length)
          continue;

        if (emittedKeys.Add(candidate))
          yield return candidate;
      }
    }
  }

  /// <summary>
  /// Возвращает ключи индекса, которые начинаются с фонетического ключа запроса.
  /// </summary>
  /// <param name="searchValue">Фонетический ключ запроса.</param>
  /// <returns>Ключи индекса с подходящим началом.</returns>
  private IEnumerable<string> EnumeratePhoneticPrefixKeys(string searchValue)
  {
    if (_searchIndex is null || _searchIndex.Count == 0)
      yield break;

    if (searchValue.Length < _minPhoneticPrefixLength)
      yield break;

    IList<string> keys = _searchIndex.Keys;

    int index = FindFirstKeyGreaterOrEqual(
        keys,
        searchValue);

    for (; index < keys.Count; index++)
    {
      string key = keys[index];

      if (!key.StartsWith(searchValue, StringComparison.Ordinal))
        yield break;

      yield return key;
    }
  }

  /// <summary>
  /// Возвращает ключи для поиска кандидатов: исходный ключ и все варианты с одной удалённой буквой.
  /// </summary>
  /// <param name="value">Фонетический ключ.</param>
  /// <returns>Ключи поиска по кандидатному индексу.</returns>
  private static IEnumerable<string> EnumerateLookupKeys(string value)
  {
    yield return value;

    for (int i = 0; i < value.Length; i++)
      yield return CreateDeletionKey(value, i);
  }

  /// <summary>
  /// Добавляет связь между ключом поиска кандидатов и исходным фонетическим ключом.
  /// </summary>
  /// <param name="candidateIndex">Индекс кандидатов.</param>
  /// <param name="candidateKey">Ключ поиска кандидатов.</param>
  /// <param name="sourceKey">Исходный фонетический ключ.</param>
  private static void AddPhoneticCandidate(
    Dictionary<string, List<string>> candidateIndex,
    string candidateKey,
    string sourceKey)
  {
    if (!candidateIndex.TryGetValue(candidateKey, out List<string>? candidates))
    {
      candidates = [];
      candidateIndex.Add(candidateKey, candidates);
    }

    if (!ContainsOrdinal(candidates, sourceKey))
      candidates.Add(sourceKey);
  }

  /// <summary>
  /// Создаёт ключ с одной удалённой буквой.
  /// </summary>
  /// <param name="value">Исходный ключ.</param>
  /// <param name="index">Позиция удаляемой буквы.</param>
  /// <returns>Ключ с удалённой буквой.</returns>
  private static string CreateDeletionKey(string value, int index)
  {
    if (value.Length == 0)
      return string.Empty;

    if (value.Length == 1)
      return string.Empty;

    if (index == 0)
      return value[1..];

    if (index == value.Length - 1)
      return value[..^1];

    return string.Concat(
      value.AsSpan(0, index),
      value.AsSpan(index + 1));
  }

  /// <summary>
  /// Проверяет наличие строки в списке с ordinal-сравнением.
  /// </summary>
  /// <param name="items">Список строк.</param>
  /// <param name="value">Искомое значение.</param>
  /// <returns>
  /// <see langword="true"/>, если значение уже есть в списке.
  /// </returns>
  private static bool ContainsOrdinal(List<string> items, string value)
  {
    for (int i = 0; i < items.Count; i++)
      if (StringComparer.Ordinal.Equals(items[i], value))
        return true;

    return false;
  }

  /// <summary>
  /// Находит первый ключ, который больше или равен искомому значению.
  /// </summary>
  /// <param name="keys">Отсортированные ключи поискового индекса.</param>
  /// <param name="value">Искомое значение.</param>
  /// <returns>Индекс первого подходящего ключа.</returns>
  private static int FindFirstKeyGreaterOrEqual(IList<string> keys, string value)
  {
    int left = 0;
    int right = keys.Count;

    Comparer<string> comparer = Comparer<string>.Default;

    while (left < right)
    {
      int middle = left + ((right - left) / 2);

      if (comparer.Compare(keys[middle], value) < 0)
        left = middle + 1;
      else
        right = middle;
    }

    return left;
  }
}