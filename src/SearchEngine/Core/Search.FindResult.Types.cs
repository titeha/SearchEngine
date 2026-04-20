namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Эффективные параметры выполнения конкретного поискового запроса.
  /// </summary>
  /// <param name="SearchType">Тип поискового механизма.</param>
  /// <param name="SearchLocation">Место поиска.</param>
  /// <param name="PrecisionSearch">Точность поиска в процентах.</param>
  /// <param name="AcceptableCountMisprint">Допустимое количество опечаток.</param>
  private readonly record struct SearchExecutionOptions(
    SearchType SearchType,
    SearchLocation SearchLocation,
    int PrecisionSearch,
    int AcceptableCountMisprint);

  /// <summary>
  /// Внутренний ранг результата для мягкого режима поиска.
  /// </summary>
  /// <param name="MatchedTerms">Количество совпавших слов.</param>
  /// <param name="TotalDistance">Суммарная дистанция.</param>
  private readonly record struct SearchRank(int MatchedTerms, int TotalDistance);
}
