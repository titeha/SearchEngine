using System.Xml.Linq;

using CommonClasses;

using static System.Math;
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

  #region Конструкторы
  static Search()
  {
    XDocument _loadDoc = XDocument.Parse(CodeKeysRus);
    foreach (XElement _item in _loadDoc.Root.Elements("key"))
      _codeKeyRus.Add(Convert.ToChar(_item.Attribute("char").Value), int.Parse(_item.Value));

    _loadDoc = XDocument.Parse(CodeKeysEng);
    foreach (XElement _item in _loadDoc.Root.Elements("key"))
      _codeKeyEng.Add(Convert.ToChar(_item.Attribute("char").Value), int.Parse(_item.Value));

    _loadDoc = XDocument.Parse(CommonKeyCodes);
    foreach (XElement _item in _loadDoc.Root.Elements("key"))
    {
      char _sym = Convert.ToChar(_item.Attribute("char").Value);
      int _code = int.Parse(_item.Value);
      _codeKeyEng.Add(_sym, _code);
      _codeKeyRus.Add(_sym, _code);
    }

    _keyCodesRUEN = _codeKeyEng.Concat(_codeKeyRus);

    _loadDoc = XDocument.Parse(DistanceCodeKey);
    foreach (XElement _item in _loadDoc.Root.Elements("key"))
    {
      int _code = int.Parse(_item.Attribute("id").Value);
      List<int> _aroundKeysCode = new();
      foreach (XElement _key in _item.Elements("near_key"))
        _aroundKeysCode.Add(int.Parse(_key.Value));
      _distanceCodeKey.Add(_code, _aroundKeysCode);
    }
  }
  #endregion

  #region Методы
  #region Статичные методы
  public static int DistanceLeventstein(string source, string target)
  {
    if (source.IsNullOrEmpty())
    {
      if (target.IsNullOrEmpty())
        return 0;
      return target.Length * 2;
    }
    if (target.IsNullOrEmpty())
      return source.Length * 2;

    int _targetLength = target.Length;
    int _sourceLength = source.Length;
    int[,] _distance = new int[3, _targetLength + 1];

    for (int j = 1; j < _targetLength; j++)
      _distance[0, j] = j * 2;

    int _currentRow = 0;

    for (int i = 1; i < source.Length; i++)
    {
      _currentRow = i % 3;
      int _previousRow = (i - 1) % 3;
      _distance[_currentRow, 0] = i * 2;
      int _compensation = i == _sourceLength ? 1 : 2;

      for (int j = 1; j <= _targetLength; j++)
      {
        _distance[_currentRow, j] = Min(
          Min(
            _distance[_previousRow, j] + _compensation,
            _distance[_currentRow, j - 1] + _compensation),
          _distance[_previousRow, j - 1] + CostDistanceSymbol(source, i - 1, target, j - 1));
        if (i > 1
            && j > 1
            && _keyCodesRUEN[source[i - 1]] == _keyCodesRUEN[target[j - 2]]
            && _keyCodesRUEN[source[i - 2]] == _keyCodesRUEN[target[j - 1]])
          _distance[_currentRow, j] = Min(_distance[_currentRow, j], _distance[(i - 2) % 3, j - 2] + 2);
      }
    }

    return _distance[_currentRow, _targetLength];
  }

  private static int CostDistanceSymbol(string source, int sourcePosition, string target, int targetPosition)
  {
    if (source[sourcePosition] == target[targetPosition])
      return 0;

    int _comparerCode = _keyCodesRUEN[source[sourcePosition]];
    if (_comparerCode != 0 && _comparerCode == _keyCodesRUEN[target[targetPosition]])
      return 0;

    if (_distanceCodeKey.TryGetValue(_comparerCode, out var _nearKeys))
      return _nearKeys.Contains(_keyCodesRUEN[target[targetPosition]]) ? 1 : 2;
    else
      return 2;
  }
  #endregion
  #endregion
}