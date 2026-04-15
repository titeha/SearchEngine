using System.Xml.Linq;

using StringFunctions;

using static SearchEngine.Properties.Resources;

namespace SearchEngine;

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

  private protected SortedList<string, IndexList<T>>? _searchIndex;
  private protected SortedSet<string> _searchList;

  private bool _isIndexComplete;
  #endregion

  #region Свойства
  /// <summary>
  /// Фонетический поиск. Исключает поиск с числами
  /// </summary>
  public bool IsPhoneticSearch { get; }

  /// <summary>
  /// Поиск с цифрами. Исключает фонетический поиск
  /// </summary>
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
  /// Точность поиска в %
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
  /// Допустимое количество опечаток для неточного поиска
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
  /// Тип поискового механизма
  /// </summary>
  public SearchType SearchType { get; set; }

  /// <summary>
  /// Место поиска
  /// </summary>
  public SearchLocation SearchLocation { get; set; }

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

  public Search()
  {
    _isNumberSearch = false;
    _precision = -1;
    _missprintCount = -1;
    _searchList = new();
    _searchIndex = new();
    _isIndexComplete = false;
  }

  public Search(bool isPhoneticSearch) : this() => IsPhoneticSearch = isPhoneticSearch;
  #endregion

  #region Методы
  [Obsolete("Используйте FindResult(...) или TryFind(...). Legacy API без Result устарел и будет удалён в следующем мажорном релизе.", false)]
  public SearchResultList<T> Find(string searchString)
  {
    static int CalculateDistance(int length, int percent) => length > 1
      ? length - (length * percent / 100)
      : 0;

    SearchResultList<T> searchResult = new();

    DisassemblyString(searchString);
    foreach (var item in _searchList)
    {
      if (IsPhoneticSearch)
        searchResult.Union(PhoneticFind(item));
      else if (item.Length == 2 || SearchType.ExactSearch == SearchType)
        searchResult.Union(ExactSearch(item));
      else
        searchResult.Union(FuzzySearch(item, AcceptableCountMisprint >= 0
          ? AcceptableCountMisprint
          : CalculateDistance(item.Length, PrecisionSearch)));
    }

    return searchResult;
  }

  private protected void DisassemblyString(string source)
  {
    _searchList.Clear();

    foreach (string searchItem in DisassembleSearchTerms(source))
      _searchList.Add(searchItem);
  }

  /// <summary>
  /// Разбирает поисковую строку в локальный набор слов,
  /// не изменяя состояние экземпляра.
  /// </summary>
  /// <param name="source">Исходная поисковая строка.</param>
  /// <returns>Упорядоченный набор пригодных для поиска слов.</returns>
  private string[] DisassembleSearchTerms(string source)
  {
    string clearedString = source.Trim().ToUpper();

    if (clearedString.IsNullOrWhiteSpace())
      return [];

    SortedSet<string> searchItems = new();
    var delimiterArray = IndexBuilder.Delimiters.ToCharArray();
    var values = clearedString.Split(delimiterArray, StringSplitOptions.RemoveEmptyEntries);

    for (int i = 0, count = values.Length; i < count; i++)
      if (values[i].Length > 1)
        searchItems.Add(IsPhoneticSearch ? PhoneticSearch.MetaPhone(values[i]) : values[i]);

    return [.. searchItems];
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

    static int CalculateResult(string searchValue, string targetString, bool origin)
    {
      int calcResult = 0;
      int tLength = targetString.Length;
      int sLength = searchValue.Length;

      if (sLength == tLength)
        calcResult = Levenshtein.DistanceLevenhstein(searchValue, targetString);
      else if (sLength < tLength)
        if (origin)
          calcResult = Levenshtein.DistanceLevenhstein(searchValue, targetString[..sLength]);
        else
        {
          int actualLength = tLength - sLength + 1;
          int[] distances = new int[actualLength];
          for (int i = 0; i < actualLength; i++)
            distances[i] = Levenshtein.DistanceLevenhstein(searchValue, targetString[i..(i + sLength)]);

          calcResult = distances.Min();
        }

      return calcResult;
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
        .Select(d => (Distance: CalculateResult(searchValue, d.Key, origin), Indexes: d.Value))
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

  private SearchResultList<T> PhoneticFind(string searchValue)
  {
    static int CalculateResult(string searchValue, string targetString)
    {
      int calcResult = 0;
      int sLength = searchValue.Length;

      if (!targetString.StartsWith(searchValue))
      {
        string checkString;
        if (sLength < targetString.Length)
          checkString = targetString[..(sLength + 1)];
        else
          checkString = searchValue;
        calcResult = Levenshtein.DistanceLevenhstein(searchValue, checkString);
      }
      return calcResult;
    }

    SearchResultList<T> searchResult = new();
    searchResult.Items.Add(0, new());
    int sLength = searchValue.Length;

    var searchValues = _searchIndex!
      .AsParallel()
      .WithDegreeOfParallelism(Environment.ProcessorCount)
      .Where(l => l.Key.Length >= sLength)
      .Select(d => (Distance: CalculateResult(searchValue, d.Key), Indexes: d.Value))
      .Where(r => r.Distance <= 1)
      .AsSequential()
      .GroupBy(v => v.Distance)
      .Select(g => (Distance: g.Key, Indexes: g
        .Select(i => i.Indexes)
        .Aggregate((a, r) => r.UnionIndexes(a))));

    foreach (var (distance, indexes) in searchValues)
      searchResult.Items[distance] = indexes;

    return searchResult;
  }

  internal void IndexPreparing()
  {
    IsIndexComplete = false;
    _searchIndex?.Clear();
  }

  private void OnCreateIndexComplete() => CreateIndexComplete?.Invoke(this, EventArgs.Empty);
  #endregion Методы

  #region События
  public event EventHandler? CreateIndexComplete;
  #endregion
}