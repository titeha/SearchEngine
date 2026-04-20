using SearchEngine.Models;

namespace SearchEngine;

/// <summary>
/// Представляет сгруппированные результаты поиска.
/// </summary>
/// <typeparam name="T">Тип идентификатора найденной записи.</typeparam>
public class SearchResultList<T> where T : struct
{
  #region Поля
  private readonly IndexList<T> _emptyList = new(true);
  #endregion

  #region Свойства
  /// <summary>
  /// Возвращает список индексов для указанного диапазона совпадения.
  /// Если диапазон отсутствует, возвращается пустой список только для чтения.
  /// </summary>
  /// <param name="range">Диапазон совпадения, сформированный поисковым движком.</param>
  public IndexList<T> this[int range] => !Items.TryGetValue(range, out IndexList<T>? value) ? _emptyList : value;

  /// <summary>
  /// Набор найденных индексов, сгруппированный по диапазонам совпадения.
  /// </summary>
  public SortedList<int, IndexList<T>> Items { get; set; }

  /// <summary>
  /// Возвращает <see langword="true"/>, если среди диапазонов есть хотя бы один непустой результат.
  /// </summary>
  public bool IsHasIndex => Items.Any(i => i.Value.Count > 0);
  #endregion

  #region Конструктор
  /// <summary>
  /// Инициализирует пустой набор результатов поиска.
  /// </summary>
  public SearchResultList() => Items = [];
  #endregion

  #region Методы
  /// <summary>
  /// Объединяет текущий набор результатов с другим набором.
  /// </summary>
  /// <param name="secondList">Набор результатов, который нужно объединить с текущим.</param>
  internal void Union(SearchResultList<T> secondList)
  {
    foreach (var item in secondList
      .Items
      .Where(value => !Items.TryAdd(value.Key, value.Value))
      .ToList())
      Items[item.Key] = Items[item.Key].UnionIndexes(item.Value);
  }
  #endregion
}
