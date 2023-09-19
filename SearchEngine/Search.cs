using System.Xml.Linq;

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

  private SortedList<string, IndexList<T>>? _searchIndex;
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
  /// Точночть поиска в %
  /// </summary>
  public int PrecissionSearch
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
  public int AcceptableCountMissprint
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

    _keyCodesRUEN = _codeKeyEng.Concat(_codeKeyRus);

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
  }

  public Search(bool isPhonecticSearch) : this() => IsPhoneticSearch = isPhonecticSearch;
  #endregion

  #region Методы

  #endregion
}