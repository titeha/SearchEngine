namespace SearchEngine;

/// <summary>
/// Правила выбора режима параллельного выполнения.
/// </summary>
internal static class ParallelExecutionPolicy
{
  /// <summary>
  /// Определяет, допустимо ли использовать параллельную обработку
  /// на текущем количестве логических процессоров.
  /// </summary>
  /// <param name="processorCount">Количество доступных логических процессоров.</param>
  /// <returns>
  /// <see langword="true"/>, если параллельная обработка допустима;
  /// иначе <see langword="false"/>.
  /// </returns>
  internal static bool CanUseParallel(int processorCount) => processorCount > 1;

  /// <summary>
  /// Возвращает безопасную степень параллелизма.
  /// </summary>
  /// <param name="processorCount">Количество доступных логических процессоров.</param>
  /// <returns>
  /// Степень параллелизма не меньше 1.
  /// Для систем, где параллельная обработка допустима, значение не меньше 2.
  /// </returns>
  internal static int GetEffectiveDegreeOfParallelism(int processorCount)
  {
    if (processorCount <= 1)
      return 1;

    return Math.Max(2, (int)Math.Floor(processorCount * 0.75d));
  }

  /// <summary>
  /// Определяет, следует ли запускать операцию в параллельном режиме.
  /// </summary>
  /// <param name="processorCount">Количество доступных логических процессоров.</param>
  /// <param name="forceParallel">Признак принудительного включения параллельной обработки.</param>
  /// <param name="itemCount">Количество обрабатываемых элементов.</param>
  /// <param name="parallelProcessingThreshold">Порог включения параллельной обработки.</param>
  /// <returns>
  /// <see langword="true"/>, если следует использовать параллельную обработку;
  /// иначе <see langword="false"/>.
  /// </returns>
  internal static bool ShouldUseParallel(
    int processorCount,
    bool forceParallel,
    int itemCount,
    int parallelProcessingThreshold)
  {
    if (!CanUseParallel(processorCount))
      return false;

    if (forceParallel)
      return true;

    return itemCount > parallelProcessingThreshold;
  }
}