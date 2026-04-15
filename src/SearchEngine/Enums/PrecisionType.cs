namespace SearchEngine;

/// <summary>
/// Определяет способ задания точности для неточного поиска.
/// </summary>
public enum PrecisionType
{
  /// <summary>
  /// Точность задаётся точным количеством допустимых опечаток.
  /// </summary>
  PrecisionTypeCount,

  /// <summary>
  /// Точность задаётся как процент от длины слова.
  /// </summary>
  PrecisionPercent
}
