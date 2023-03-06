using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using static SearchEngine.Properties.Resources;

namespace SearchEngine;

public partial class Search<T> where T : struct
{
  #region Структуры для кодов клавиш
  private static readonly SortedList<char, int> _codeKeyRus = new();
  private static readonly SortedList<char, int> _codeKeyEng = new();
  private static readonly SortedList<char, int> _codeKeyRUEN;
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

	_codeKeyRUEN = _codeKeyEng.Concat(_codeKeyRus);

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
}