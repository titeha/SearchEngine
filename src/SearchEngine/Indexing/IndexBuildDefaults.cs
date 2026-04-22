namespace SearchEngine;

/// <summary>
/// Содержит значения по умолчанию для построения поискового индекса.
/// </summary>
internal static class IndexBuildDefaults
{
  /// <summary>
  /// Минимальный размер источника, начиная с которого автоматически допускается
  /// параллельное построение индекса.
  /// </summary>
  internal const int ParallelProcessingThreshold = 100_000;
}