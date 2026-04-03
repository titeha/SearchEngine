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
  private int _precission;
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
    get => _precission;
    set
    {
      if (_precission != value && value >= 0 && value <= 100)
        _precission = value;
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
    _precission = -1;
    _missprintCount = -1;
    _searchList = new();
    _searchIndex = new();
    _isIndexComplete = false;
  }

  public Search(bool isPhoneticSearch) : this() => IsPhoneticSearch = isPhoneticSearch;
  #endregion

  #region Методы
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
        searchResult.Union(FusySearch(item, AcceptableCountMisprint >= 0
          ? AcceptableCountMisprint
          : CalculateDistance(item.Length, PrecisionSearch)));
    }

    return searchResult;
  }

  private protected void DisassemblyString(string source)
  {
    string clearedString = source.Trim().ToUpper();
    _searchList.Clear();

    if (!clearedString.IsNullOrWhiteSpace())
    {
      var delimiterArray = IndexBuilder.Delimiters.ToCharArray();
      var values = clearedString.Split(delimiterArray, StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0, count = values.Length; i < count; i++)
        if (values[i].Length > 1)
          _searchList.Add(IsPhoneticSearch ? PhoneticSearch.MetaPhone(values[i]) : values[i]);
    }
  }

  private SearchResultList<T> ExactSearch(string searchValue)
  {
    SearchResultList<T> searchResult = new();
    searchResult.Items.Add(0, new IndexList<T>());
    bool origin = SearchLocation.BeginWord == SearchLocation;
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

  private SearchResultList<T> FusySearch(string searchValue, int distance)
  {
    SearchResultList<T> searchResult = new();
    bool origin = SearchLocation.BeginWord == SearchLocation;
    int sLength = searchValue.Length;

    static int CalculateResult(string searchValue, string targetString, bool origin)
    {
      int calcResult = 0;
      int tLength = targetString.Length;
      int sLength = searchValue.Length;

      if (sLength == tLength)
        calcResult = Levenshtein.DistanceLeventstein(searchValue, targetString);
      else if (sLength < tLength)
        if (origin)
          calcResult = Levenshtein.DistanceLeventstein(searchValue, targetString[..sLength]);
        else
        {
          int actualLength = tLength - sLength + 1;
          int[] distances = new int[actualLength];
          for (int i = 0; i < actualLength; i++)
            distances[i] = Levenshtein.DistanceLeventstein(searchValue, targetString[i..sLength]);

          calcResult = distances.Min();
        }

      return calcResult;
    }

    if (distance == 0)
      searchResult = ExactSearch(searchValue);
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
        calcResult = Levenshtein.DistanceLeventstein(searchValue, checkString);
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
      searchResult[distance].UnionIndexes(indexes);

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