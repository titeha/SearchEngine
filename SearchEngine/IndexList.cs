namespace SearchEngine;

/// <summary>
/// Список найденных индексов
/// </summary>
/// <typeparam name="T">Целый числовой тип</typeparam>
public class IndexList<T> where T : struct
{
  #region Поля
  private protected readonly List<T> _indexes;
  #endregion

  #region Свойства
  internal int Count => _indexes.Count;

  internal bool IsReadOnly { get; }
  #endregion

  #region Конструкторы
  public IndexList() => _indexes = new(4);

  public IndexList(T newIndex) : this() => _indexes.Add(newIndex);

  internal IndexList(IEnumerable<T> indexes)
  {
    _indexes = new(indexes);
    _indexes.Sort();
  }

  internal IndexList(bool isReadOnly) : this() => IsReadOnly = isReadOnly;
  #endregion

  #region Методы
  public override string ToString() => string.Join(',', _indexes);

  /// <summary>
  /// Добавление нового элемента в список индексов
  /// </summary>
  /// <param name="newIndex">Добавляемый индекс</param>
  public void TryAddValue(T newIndex)
  {
    if (!(IsReadOnly || _indexes.Contains(newIndex)))
    {
      _indexes.Add(newIndex);
      _indexes.Sort();
    }
  }

  public IndexList<T> UnionIndexes(IndexList<T> otherIndexes) => new(_indexes.Union(otherIndexes._indexes));

  internal bool Contains(IndexList<T> lookUpSet) => _indexes.Intersect(lookUpSet._indexes) is not null;
  #endregion
}