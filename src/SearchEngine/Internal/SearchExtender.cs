namespace SearchEngine;

internal static class SearchExtender
{
  public static SortedList<T, V> Concatenate<T, V>(this SortedList<T, V> source, SortedList<T, V> added) where T : notnull
  {
    SortedList<T, V> result = new(source);

    foreach (var item in added)
      result[item.Key] = item.Value;

    return result;
  }
}