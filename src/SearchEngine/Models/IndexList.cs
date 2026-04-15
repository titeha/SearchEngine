namespace SearchEngine;

/// <summary>
/// Представляет список индексов найденных записей.
/// </summary>
/// <typeparam name="T">Тип идентификатора записи.</typeparam>
public class IndexList<T> where T : struct
{
  #region Поля
  private protected readonly List<T> _indexes;
  #endregion

  #region Свойства
  internal int Count => _indexes.Count;

  internal bool IsReadOnly { get; }

  /// <summary>
  /// Возвращает элементы списка индексов.
  /// </summary>
  public IEnumerable<T> Items => _indexes;
  #endregion

  #region Конструкторы
  /// <summary>
  /// Инициализирует пустой список индексов.
  /// </summary>
  public IndexList() => _indexes = new(4);

  /// <summary>
  /// Инициализирует список индексов одним значением.
  /// </summary>
  /// <param name="newIndex">Начальный индекс.</param>
  public IndexList(T newIndex) : this() => _indexes.Add(newIndex);

  internal IndexList(IEnumerable<T> indexes)
  {
    _indexes = new(indexes);
    _indexes.Sort();
  }

  internal IndexList(bool isReadOnly) : this() => IsReadOnly = isReadOnly;
  #endregion

  #region Методы
  /// <summary>
  /// Возвращает строковое представление списка индексов.
  /// </summary>
  /// <returns>Строка со значениями индексов, разделёнными запятыми.</returns>
  public override string ToString() => string.Join(',', _indexes);

  /// <summary>
  /// Пытается добавить новый индекс в список.
  /// </summary>
  /// <param name="newIndex">Индекс, который нужно добавить.</param>
  public void TryAddValue(T newIndex)
  {
    if (!(IsReadOnly || _indexes.Contains(newIndex)))
    {
      _indexes.Add(newIndex);
      _indexes.Sort();
    }
  }

  /// <summary>
  /// Объединяет текущий список индексов с другим списком.
  /// </summary>
  /// <param name="otherIndexes">Список индексов для объединения.</param>
  /// <returns>Новый список индексов после объединения.</returns>
  public IndexList<T> UnionIndexes(IndexList<T> otherIndexes)
  {
    if (_indexes.Count > 0)
      return new(_indexes.Intersect(otherIndexes._indexes));
    else
    {
      _indexes.AddRange(otherIndexes._indexes);
      return new(_indexes);
    }
  }

  /// <summary>
  /// Объединяет текущий список индексов с несколькими списками.
  /// </summary>
  /// <param name="otherIndexes">Набор списков индексов для объединения.</param>
  /// <returns>Новый список индексов после объединения.</returns>
  public IndexList<T> UnionIndexes(IEnumerable<IndexList<T>> otherIndexes)
  {
    var union = otherIndexes.SelectMany(o => o._indexes).Distinct();
    if (_indexes.Count > 0)
      return new(_indexes.Intersect(union));
    else
    {
      _indexes.AddRange(union);
      return new(_indexes);
    }
  }

  internal bool Contains(IndexList<T> lookUpSet) => _indexes.Intersect(lookUpSet._indexes)?.Count() > 0;
  #endregion
}
