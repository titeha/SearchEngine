namespace SearchEngine.Models;

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

  /// <summary>
  /// Получает внутреннее представление индексов без создания копии.
  /// </summary>
  /// <remarks>
  /// Свойство предназначено только для внутренних алгоритмов объединения результатов.
  /// Наружу библиотека по-прежнему отдаёт публичное перечисление через <see cref="Items"/>.
  /// </remarks>
  internal IReadOnlyList<T> InternalItems => _indexes;
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
    _indexes = [.. indexes];
    _indexes.Sort();
  }

  internal IndexList(bool isReadOnly) : this() => IsReadOnly = isReadOnly;

  internal IndexList(List<T> indexes, bool sort)
  {
    _indexes = indexes;

    if (sort)
      _indexes.Sort();
  }
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
    if (_indexes.Count > 0)
    {
      HashSet<T> union = [];

      foreach (IndexList<T> otherIndex in otherIndexes)
        foreach (T index in otherIndex._indexes)
          union.Add(index);

      List<T> intersection = [];

      foreach (T index in _indexes)
        if (union.Contains(index))
          intersection.Add(index);

      return new IndexList<T>(intersection, sort: false);
    }

    using IEnumerator<IndexList<T>> enumerator = otherIndexes.GetEnumerator();

    if (!enumerator.MoveNext())
      return new IndexList<T>();

    IndexList<T> first = enumerator.Current;

    if (!enumerator.MoveNext())
      return first.Clone();

    HashSet<T> result = [];

    foreach (T index in first._indexes)
      result.Add(index);

    do
    {
      foreach (T index in enumerator.Current._indexes)
        result.Add(index);
    }
    while (enumerator.MoveNext());

    List<T> indexes = [.. result];
    indexes.Sort();

    return new IndexList<T>(indexes, sort: false);
  }

  internal bool Contains(IndexList<T> lookUpSet) => _indexes.Intersect(lookUpSet._indexes)?.Count() > 0;

  internal IndexList<T> Clone() => new IndexList<T>([.. _indexes], sort: false);
  #endregion
}
