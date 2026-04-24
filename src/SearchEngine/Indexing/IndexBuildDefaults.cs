namespace SearchEngine;

/// <summary>
/// Содержит значения по умолчанию для построения поискового индекса.
/// </summary>
internal static class IndexBuildDefaults
{
  /// <param name="parallelProcessingThreshold">
  /// Минимальный размер набора данных, начиная с которого допускается
  /// автоматический переход к параллельной обработке.
  /// По умолчанию автопараллельность отключена, чтобы не увеличивать давление
  /// на память на рабочих и desktop-сценариях.
  /// </param>
  internal const int _parallelProcessingThreshold = int.MaxValue;
}