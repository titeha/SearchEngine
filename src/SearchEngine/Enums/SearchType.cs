namespace SearchEngine;

/// <summary>
/// Определяет используемый алгоритм поиска.
/// </summary>
public enum SearchType
{
  /// <summary>
  /// Точный поиск без допуска опечаток.
  /// </summary>
  ExactSearch,

  /// <summary>
  /// Неточный поиск с допуском опечаток.
  /// </summary>
  NearSearch
}
