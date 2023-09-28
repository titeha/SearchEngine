namespace SearchEngine;

public class SearchResultList<T> where T : struct
{
  #region Поля
  private readonly IndexList<T> _emptyList = new(true);
  #endregion

  #region Свойства
  public IndexList<T> this[int range] => !Items.ContainsKey(range) ? _emptyList : Items[range];

  public SortedList<int, IndexList<T>> Items { get; set; }

  public bool IsHasIndex => Items.Any(i => i.Value.Count > 0);
  #endregion

  #region Конструктор
  public SearchResultList() => Items = new();
  #endregion

  #region Методы
  internal void Union(SearchResultList<T> secondList)
  {
    foreach (var item in secondList.Items.Where(value => !Items.TryAdd(value.Key, value.Value)))
      Items[item.Key].UnionIndexes(item.Value);
  }
  #endregion
}