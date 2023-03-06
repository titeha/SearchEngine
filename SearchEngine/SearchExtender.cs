namespace SearchEngine;

internal static class SearchExtender
{
  public static SortedList<T, V> Concat<T, V>(this SortedList<T, V> source, SortedList<T, V> added)
  {
	SortedList<T, V> _result = new(source);
	foreach (var _item in added)
	  _result[_item.Key] = _item.Value;

	return _result;
  }
}