namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Находит самый короткий список идентификаторов.
  /// </summary>
  /// <param name="indexLists">Списки идентификаторов.</param>
  /// <returns>Индекс самого короткого списка.</returns>
  private static int FindSmallestIndexList(
    IReadOnlyList<T>[] indexLists)
  {
    int smallestIndex = 0;
    int smallestCount = indexLists[0].Count;

    for (int i = 1; i < indexLists.Length; i++)
    {
      if (indexLists[i].Count >= smallestCount)
        continue;

      smallestIndex = i;
      smallestCount = indexLists[i].Count;
    }

    return smallestIndex;
  }

  /// <summary>
  /// Проверяет, содержат ли два отсортированных списка одинаковые значения.
  /// </summary>
  /// <param name="left">Первый список.</param>
  /// <param name="right">Второй список.</param>
  /// <returns>
  /// <see langword="true"/>, если списки имеют одинаковую длину и одинаковые элементы.
  /// </returns>
  private static bool AreSortedListsEqual(
    IReadOnlyList<T> left,
    IReadOnlyList<T> right)
  {
    if (left.Count != right.Count)
      return false;

    Comparer<T> comparer = Comparer<T>.Default;

    for (int i = 0; i < left.Count; i++)
      if (comparer.Compare(left[i], right[i]) != 0)
        return false;

    return true;
  }

  /// <summary>
  /// Строит пересечение нескольких отсортированных списков без промежуточных результатов.
  /// </summary>
  /// <param name="indexLists">Списки идентификаторов.</param>
  /// <returns>Отсортированный список общих идентификаторов.</returns>
  private static List<T> IntersectAllSortedLists(
    IReadOnlyList<T>[] indexLists)
  {
    int smallestIndex = FindSmallestIndexList(indexLists);
    IReadOnlyList<T> smallest = indexLists[smallestIndex];

    List<T>? result = null;

    int[] positions = new int[indexLists.Length];
    Comparer<T> comparer = Comparer<T>.Default;

    bool hasLastValue = false;
    T lastValue = default;

    for (int i = 0; i < smallest.Count; i++)
    {
      T candidate = smallest[i];

      if (hasLastValue && comparer.Compare(lastValue, candidate) == 0)
        continue;

      bool foundInAllLists = true;

      for (int listIndex = 0; listIndex < indexLists.Length; listIndex++)
      {
        if (listIndex == smallestIndex)
          continue;

        IReadOnlyList<T> currentList = indexLists[listIndex];

        int position = AdvanceWhileLessThan(
          currentList,
          positions[listIndex],
          candidate,
          comparer);

        positions[listIndex] = position;

        if (
          position >= currentList.Count ||
          comparer.Compare(currentList[position], candidate) != 0)
        {
          foundInAllLists = false;
          break;
        }
      }

      if (!foundInAllLists)
        continue;

      result ??= [];
      result.Add(candidate);
      lastValue = candidate;
      hasLastValue = true;
    }

    return result ?? [];
  }

  /// <summary>
  /// Строит объединение двух отсортированных списков без дублей.
  /// </summary>
  /// <param name="left">Первый список.</param>
  /// <param name="right">Второй список.</param>
  /// <returns>Отсортированный список уникальных элементов.</returns>
  private static List<T> UnionSortedLists(
    IReadOnlyList<T> left,
    IReadOnlyList<T> right)
  {
    List<T> result = new(left.Count + right.Count);

    Comparer<T> comparer = Comparer<T>.Default;

    int leftIndex = 0;
    int rightIndex = 0;

    while (leftIndex < left.Count && rightIndex < right.Count)
    {
      T leftValue = left[leftIndex];
      T rightValue = right[rightIndex];

      int comparison = comparer.Compare(leftValue, rightValue);

      if (comparison == 0)
      {
        AddIfDifferentFromLast(result, leftValue, comparer);

        leftIndex++;
        rightIndex++;

        continue;
      }

      if (comparison < 0)
      {
        AddIfDifferentFromLast(result, leftValue, comparer);
        leftIndex++;
      }
      else
      {
        AddIfDifferentFromLast(result, rightValue, comparer);
        rightIndex++;
      }
    }

    while (leftIndex < left.Count)
    {
      AddIfDifferentFromLast(result, left[leftIndex], comparer);
      leftIndex++;
    }

    while (rightIndex < right.Count)
    {
      AddIfDifferentFromLast(result, right[rightIndex], comparer);
      rightIndex++;
    }

    return result;
  }

  /// <summary>
  /// Строит объединение нескольких отсортированных списков без дублей.
  /// </summary>
  /// <param name="indexLists">Списки идентификаторов.</param>
  /// <param name="capacity">
  /// Начальная ёмкость результирующего списка.
  /// </param>
  /// <returns>Отсортированный список уникальных идентификаторов.</returns>
  private static List<T> UnionAllSortedLists(
    IReadOnlyList<IReadOnlyList<T>> indexLists,
    int capacity)
  {
    List<T> result = new(capacity);

    int[] positions = new int[indexLists.Count];
    Comparer<T> comparer = Comparer<T>.Default;

    while (TryGetMinimalCurrentValue(
      indexLists,
      positions,
      comparer,
      out T currentValue))
    {
      result.Add(currentValue);

      for (int i = 0; i < indexLists.Count; i++)
      {
        positions[i] = AdvanceWhileEqual(
          indexLists[i],
          positions[i],
          currentValue,
          comparer);
      }
    }

    return result;
  }

  /// <summary>
  /// Добавляет значение в список, если оно отличается от последнего добавленного значения.
  /// </summary>
  /// <param name="items">Список значений.</param>
  /// <param name="value">Добавляемое значение.</param>
  /// <param name="comparer">Сравниватель значений.</param>
  private static void AddIfDifferentFromLast(
    List<T> items,
    T value,
    Comparer<T> comparer)
  {
    if (items.Count == 0)
    {
      items.Add(value);
      return;
    }

    if (comparer.Compare(items[^1], value) != 0)
      items.Add(value);
  }

  /// <summary>
  /// Получает минимальное текущее значение среди нескольких отсортированных списков.
  /// </summary>
  /// <param name="indexLists">Списки идентификаторов.</param>
  /// <param name="positions">Текущие позиции в списках.</param>
  /// <param name="comparer">Сравниватель значений.</param>
  /// <param name="value">Минимальное текущее значение.</param>
  /// <returns>
  /// <see langword="true"/>, если минимальное значение найдено.
  /// </returns>
  private static bool TryGetMinimalCurrentValue(
    IReadOnlyList<IReadOnlyList<T>> indexLists,
    IReadOnlyList<int> positions,
    Comparer<T> comparer,
    out T value)
  {
    value = default;

    bool hasValue = false;

    for (int i = 0; i < indexLists.Count; i++)
    {
      IReadOnlyList<T> indexes = indexLists[i];

      if (positions[i] >= indexes.Count)
        continue;

      T currentValue = indexes[positions[i]];

      if (!hasValue || comparer.Compare(currentValue, value) < 0)
      {
        value = currentValue;
        hasValue = true;
      }
    }

    return hasValue;
  }

  /// <summary>
  /// Продвигает позицию в отсортированном списке, пока текущее значение меньше искомого.
  /// </summary>
  /// <param name="items">Отсортированный список.</param>
  /// <param name="position">Начальная позиция.</param>
  /// <param name="value">Искомое значение.</param>
  /// <param name="comparer">Сравниватель значений.</param>
  /// <returns>Новая позиция.</returns>
  private static int AdvanceWhileLessThan(
    IReadOnlyList<T> items,
    int position,
    T value,
    Comparer<T> comparer)
  {
    while (
      position < items.Count &&
      comparer.Compare(items[position], value) < 0)
    {
      position++;
    }

    return position;
  }

  /// <summary>
  /// Продвигает позицию в отсортированном списке, пока текущее значение равно указанному.
  /// </summary>
  /// <param name="items">Отсортированный список.</param>
  /// <param name="position">Начальная позиция.</param>
  /// <param name="value">Значение, которое нужно пропустить.</param>
  /// <param name="comparer">Сравниватель значений.</param>
  /// <returns>Новая позиция.</returns>
  private static int AdvanceWhileEqual(
    IReadOnlyList<T> items,
    int position,
    T value,
    Comparer<T> comparer)
  {
    while (
      position < items.Count &&
      comparer.Compare(items[position], value) == 0)
    {
      position++;
    }

    return position;
  }
}
