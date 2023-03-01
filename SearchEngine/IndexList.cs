namespace SearchEngine;

/// <summary>
/// Список найденных индексов
/// </summary>
/// <typeparam name="T">Целый числовой тип</typeparam>
internal class IndexList<T> where T : struct
{
  #region Поле
  private protected readonly List<T> _indexes;
  #endregion

  #region Свойства
  internal int Count => _indexes.Count;
  #endregion

  #region Конструкторы
  public IndexList() => _indexes = new List<T>(4);

  public IndexList(T newIndex) : this() => _indexes.Add(newIndex);

  private IndexList(IEnumerable<T> indexes)
  {
    _indexes = new List<T>(indexes);
    _indexes.Sort();
  }
  #endregion

  #region Методы
  public override string ToString() => string.Join(',', _indexes);

  public void TryAddValue(T newIndex)
  {
    if (!_indexes.Contains(newIndex))
    {
      _indexes.Add(newIndex);
      _indexes.Sort();
    }
  }

  public IndexList<T> UnionIndexes(IndexList<T> otherIndexes) => new(_indexes.Union(otherIndexes._indexes));
  #endregion
}