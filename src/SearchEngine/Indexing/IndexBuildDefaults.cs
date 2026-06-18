namespace SearchEngine;

/// <summary>
/// Содержит значения по умолчанию для построения поискового индекса.
/// </summary>
internal static class IndexBuildDefaults
{
  /// <summary>
  /// Минимальный размер набора данных, начиная с которого допускается
  /// автоматический переход к параллельной обработке.
  /// По умолчанию автопараллельность отключена, чтобы не увеличивать давление
  /// на память на рабочих и desktop-сценариях.
  /// </summary>
  internal const int _parallelProcessingThreshold = int.MaxValue;
}