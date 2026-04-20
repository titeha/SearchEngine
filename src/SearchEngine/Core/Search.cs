using System.Xml.Linq;

using SearchEngine.Models;

using StringFunctions;

using static SearchEngine.Properties.Resources;

namespace SearchEngine;

/// <summary>
/// Представляет поисковый движок для точного, нечёткого и фонетического поиска
/// по заранее подготовленному строковому индексу.
/// </summary>
/// <remarks>
/// Обычный жизненный цикл экземпляра:
/// создать объект, подготовить индекс через <c>PrepareIndexResult(...)</c>,
/// затем выполнять поиск через <c>FindResult(...)</c>.
/// <para>
/// Экземпляр не следует одновременно использовать для перестроения индекса,
/// изменения настроек поиска и выполнения поиска из разных потоков.
/// </para>
/// </remarks>
public partial class Search<T> where T : struct
{
  #region Структуры для кодов клавиш
  private static readonly SortedList<char, int> _codeKeyRus = new();
  private static readonly SortedList<char, int> _codeKeyEng = new();
  private static readonly SortedList<char, int> _keyCodesRUEN;
  private static readonly SortedList<int, List<int>> _distanceCodeKey = new();
  #endregion

  #region Поля
  private bool _isNumberSearch;
  private int _precision;
  private int _missprintCount;

  /// <summary>
  /// Индекс фонетических ключей-кандидатов для поиска на расстоянии одной правки.
  /// </summary>
  private Dictionary<string, List<string>>? _phoneticCandidateIndex;
  private protected SortedList<string, IndexList<T>>? _searchIndex;
  private protected SortedSet<string> _searchList;

  private bool _isIndexComplete;
  #endregion

  #region Свойства
  /// <summary>
  /// Получает значение, указывающее, включён ли фонетический поиск.
  /// </summary>
  public bool IsPhoneticSearch { get; }

  /// <summary>
  /// Получает или задаёт значение, указывающее, разрешён ли поиск по числовым данным.
  /// </summary>
  /// <remarks>
  /// Числовой поиск не используется вместе с фонетическим режимом.
  /// </remarks>
  public bool IsNumberSearch
  {
    get => _isNumberSearch;
    set
    {
      if (_isNumberSearch != value && !IsPhoneticSearch)
        _isNumberSearch = value;
    }
  }

  /// <summary>
  /// Получает или задаёт точность нечёткого поиска в процентах.
  /// </summary>
  public int PrecisionSearch
  {
    get => _precision;
    set
    {
      if (_precision != value && value >= 0 && value <= 100)
        _precision = value;
    }
  }

  /// <summary>
  /// Получает или задаёт допустимое количество опечаток для нечёткого поиска.
  /// </summary>
  public int AcceptableCountMisprint
  {
    get => _missprintCount;
    set
    {
      if (_missprintCount != value && value >= 0)
        _missprintCount = value;
    }
  }

  /// <summary>
  /// Получает или задаёт тип поискового механизма.
  /// </summary>
  public SearchType SearchType { get; set; }

  /// <summary>
  /// Получает или задаёт место поиска внутри слова.
  /// </summary>
  public SearchLocation SearchLocation { get; set; }

  /// <summary>
  /// Получает значение, указывающее, завершено ли построение индекса.
  /// </summary>
  public bool IsIndexComplete
  {
    get => _isIndexComplete;
    set
    {
      _isIndexComplete = value;
      OnCreateIndexComplete();
    }
  }
  #endregion

  #region Конструкторы
  static Search()
  {
    XDocument loadDoc = XDocument.Parse(CodeKeysRus);
    foreach (XElement item in loadDoc.Root!.Elements("key"))
      _codeKeyRus.Add(Convert.ToChar(item.Attribute("char")!.Value), int.Parse(item.Value));

    loadDoc = XDocument.Parse(CodeKeysEng);
    foreach (XElement item in loadDoc.Root!.Elements("key"))
      _codeKeyEng.Add(Convert.ToChar(item.Attribute("char")!.Value), int.Parse(item.Value));

    loadDoc = XDocument.Parse(CommonKeyCodes);
    foreach (XElement item in loadDoc.Root!.Elements("key"))
    {
      char _sym = Convert.ToChar(item.Attribute("char")!.Value);
      int _code = int.Parse(item.Value);
      _codeKeyEng.Add(_sym, _code);
      _codeKeyRus.Add(_sym, _code);
    }

    _keyCodesRUEN = _codeKeyEng.Concatenate(_codeKeyRus);

    loadDoc = XDocument.Parse(DistanceCodeKey);
    foreach (XElement item in loadDoc.Root!.Elements("key"))
    {
      int code = int.Parse(item.Attribute("id")!.Value);
      List<int> aroundKeysCode = new();
      foreach (XElement _key in item.Elements("near_key"))
        aroundKeysCode.Add(int.Parse(_key.Value));
      _distanceCodeKey.Add(code, aroundKeysCode);
    }
  }

  /// <summary>
  /// Создаёт экземпляр поискового движка со стандартными настройками.
  /// </summary>
  public Search()
  {
    _isNumberSearch = false;
    _precision = -1;
    _missprintCount = -1;
    _searchList = new();
    _searchIndex = new();
    _isIndexComplete = false;
  }

  /// <summary>
  /// Создаёт экземпляр поискового движка и задаёт режим фонетического поиска.
  /// </summary>
  /// <param name="isPhoneticSearch">
  /// <see langword="true"/>, если нужно использовать фонетический поиск.
  /// </param>
  public Search(bool isPhoneticSearch) : this() => IsPhoneticSearch = isPhoneticSearch;
  #endregion

  #region Методы
  private protected void DisassemblyString(string source)
  {
    _searchList.Clear();

    foreach (string searchItem in DisassembleSearchTerms(source))
      _searchList.Add(searchItem);
  }

  private SearchResultList<T> ExactSearch(string searchValue) => ExactSearch(searchValue, SearchLocation);

  private SearchResultList<T> ExactSearch(string searchValue, SearchLocation searchLocation)
  {
    SearchResultList<T> searchResult = new();
    searchResult.Items.Add(0, new IndexList<T>());
    bool origin = SearchLocation.BeginWord == searchLocation;
    int sLength = searchValue.Length;

    var result = _searchIndex!
      .AsParallel()
      .WithDegreeOfParallelism(Environment.ProcessorCount)
      .Where(l => l.Key.Length >= sLength)
      .Select(i => (Position: i.Key.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase), Indexes: i.Value))
      .Where(c => c.Position == 0 && origin || !origin && c.Position >= 0)
      .AsSequential()
      .Select(r => r.Indexes);

    searchResult.Items[0] = searchResult[0].UnionIndexes(result);

    return searchResult;
  }

  private SearchResultList<T> FuzzySearch(string searchValue, int distance) => FuzzySearch(searchValue, distance, SearchLocation);

  private SearchResultList<T> FuzzySearch(string searchValue, int distance, SearchLocation searchLocation)
  {
    SearchResultList<T> searchResult = new();
    bool origin = SearchLocation.BeginWord == searchLocation;
    int sLength = searchValue.Length;

    static int CalculateResult(string searchValue, string targetString, bool origin, int maxDistance)
    {
      int targetLength = targetString.Length;
      int searchLength = searchValue.Length;

      if (searchLength > targetLength)
        return maxDistance + 1;

      ReadOnlySpan<char> searchSpan = searchValue.AsSpan();

      if (searchLength == targetLength)
        return Levenshtein.DistanceLevenshtein(searchSpan, targetString.AsSpan(), maxDistance);

      if (origin)
        return Levenshtein.DistanceLevenshtein(searchSpan, targetString.AsSpan(0, searchLength), maxDistance);

      int bestDistance = maxDistance + 1;
      int windowCount = targetLength - searchLength + 1;

      for (int i = 0; i < windowCount; i++)
      {
        int currentDistance = Levenshtein.DistanceLevenshtein(
          searchSpan,
          targetString.AsSpan(i, searchLength),
          maxDistance);

        if (currentDistance < bestDistance)
          bestDistance = currentDistance;

        if (bestDistance == 0)
          break;
      }

      return bestDistance;
    }

    if (distance == 0)
      searchResult = ExactSearch(searchValue, searchLocation);
    else
    {
      var result = _searchIndex!
#if !DEBUG
        .AsParallel()
        .WithDegreeOfParallelism(Environment.ProcessorCount)
#endif
        .Where(s => s.Key.Length >= sLength)
        .Select(d => (Distance: CalculateResult(searchValue, d.Key, origin, distance), Indexes: d.Value))
        .Where(r => r.Distance <= distance)
#if !DEBUG
        .AsSequential()
#endif
        .GroupBy(i => i.Distance)
        .Select(g => (Distance: g.Key, Indexes: g
          .Select(x => x.Indexes)
          .Aggregate((a, r) => r.UnionIndexes(a))));

      foreach (var (Distance, Indexes) in result)
        searchResult.Items.Add(Distance, Indexes);
    }

    return searchResult;
  }

  /// <summary>
  /// Выполняет фонетический поиск по подготовленному индексу.
  /// </summary>
  /// <param name="searchValue">Фонетически нормализованное слово запроса.</param>
  /// <returns>Результат фонетического поиска.</returns>
  private SearchResultList<T> PhoneticFind(string searchValue)
  {
    SearchResultList<T> searchResult = new();

    searchResult.Items.Add(0, new IndexList<T>());

    if (_searchIndex is null || _searchIndex.Count == 0)
      return searchResult;

    int searchLength = searchValue.Length;

    List<T>? exactIndexes = null;
    List<T>? nearIndexes = null;

    foreach (string targetString in EnumeratePhoneticCandidateKeys(searchValue))
    {
      if (_searchIndex is null)
        break;

      if (!_searchIndex.TryGetValue(targetString, out IndexList<T>? indexList))
        continue;

      int distance = CalculatePhoneticDistance(
        searchValue,
        targetString);

      if (distance > 1)
        continue;

      IReadOnlyList<T> indexes = indexList.InternalItems;

      if (indexes.Count == 0)
        continue;

      if (distance == 0)
        exactIndexes = exactIndexes is null ? [.. indexes] : UnionSortedLists(exactIndexes, indexes);
      else
        nearIndexes = nearIndexes is null ? [.. indexes] : UnionSortedLists(nearIndexes, indexes);
    }

    if (exactIndexes is { Count: > 0 })
      searchResult.Items[0] = new IndexList<T>(exactIndexes, sort: false);

    if (nearIndexes is { Count: > 0 })
      searchResult.Items[1] = new IndexList<T>(nearIndexes, sort: false);

    return searchResult;

    static int CalculatePhoneticDistance(
      string searchValue,
      string targetString)
    {
      if (targetString.StartsWith(searchValue, StringComparison.Ordinal))
        return 0;

      int searchLength = searchValue.Length;

      ReadOnlySpan<char> searchSpan = searchValue.AsSpan();

      ReadOnlySpan<char> targetSpan = targetString.Length > searchLength
        ? targetString.AsSpan(0, searchLength + 1)
        : targetString.AsSpan();

      return Levenshtein.DistanceLevenshteinUpToOne(
        searchSpan,
        targetSpan);
    }
  }

  internal void IndexPreparing()
  {
    IsIndexComplete = false;

    _searchIndex?.Clear();

    _phoneticCandidateIndex?.Clear();
    _phoneticCandidateIndex = null;
  }

  private void OnCreateIndexComplete() => CreateIndexComplete?.Invoke(this, EventArgs.Empty);
  #endregion Методы

  #region События
  /// <summary>
  /// Происходит после завершения построения поискового индекса.
  /// </summary>
  public event EventHandler? CreateIndexComplete;
  #endregion
}