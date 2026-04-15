namespace SearchEngine;

/// <summary>
/// Определяет, где внутри слова допустимо искать совпадение.
/// </summary>
public enum SearchLocation
{
  /// <summary>
  /// Поиск выполняется только с начала слова.
  /// </summary>
  BeginWord,

  /// <summary>
  /// Поиск выполняется по всему слову, включая его начало.
  /// </summary>
  InWord
}
