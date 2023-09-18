namespace SearchEngine;

internal static class SearchExtender
{
  public static SortedList<T, V> Concat<T, V, Z>(this SortedList<T, V> source, SortedList<T, V> added)
  where T : struct
  where V : IndexList<Z>
  where Z : struct
  {
    SortedList<T, V> result = new(source);
    foreach (var item in added)
      if (result.ContainsKey(item.Key))
        result[item.Key] = result[item.Key].UnionIndexes(item.Value) as V;
      else
        result[item.Key] = item.Value;

    return result;
  }

  public static SortedList<T, V> Concat<T, V>(this SortedList<T, V> source, SortedList<T, V> added)
  {
    SortedList<T, V> result = new(source);

    foreach (var item in added)
      result[item.Key] = item.Value;

    return result;
  }
}