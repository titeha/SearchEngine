namespace SearchEngine;

public enum PrecisionType
{
  /// <summary>
  /// Точное количество опечаток
  /// </summary>
  PrecisionTypeCount,

  /// <summary>
  /// Количество опечаток в процентом отношении к длине слова
  /// </summary>
  PrecisionPercent
}