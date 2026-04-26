namespace SearchEngine.Benchmarks;

/// <summary>
/// Определяет фонетический алгоритм для бенчмарков.
/// </summary>
public enum PhoneticAlgorithmBenchMode
{
  /// <summary>
  /// Текущая реализация MetaPhone.
  /// </summary>
  MetaPhone,

  /// <summary>
  /// Экспериментальная реализация BMPM.
  /// </summary>
  Bmpm
}