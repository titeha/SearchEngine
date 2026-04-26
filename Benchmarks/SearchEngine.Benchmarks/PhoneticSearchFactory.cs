namespace SearchEngine.Benchmarks;

/// <summary>
/// Создаёт поисковые движки с нужным фонетическим алгоритмом для бенчмарков.
/// </summary>
internal static class PhoneticSearchFactory
{
  /// <summary>
  /// Создаёт поисковый движок с выбранным фонетическим алгоритмом.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора индексируемой записи.</typeparam>
  /// <param name="mode">Режим фонетического алгоритма.</param>
  /// <returns>Поисковый движок с включённым фонетическим поиском.</returns>
  public static Search<T> Create<T>(PhoneticAlgorithmBenchMode mode)
      where T : struct
  {
    return mode switch
    {
      PhoneticAlgorithmBenchMode.MetaPhone => new Search<T>(isPhoneticSearch: true),
      PhoneticAlgorithmBenchMode.Bmpm => new Search<T>(isPhoneticSearch: true, phoneticKeyEncoder: BmpmPhoneticEncoder.Encode),
      PhoneticAlgorithmBenchMode.BmpmApprox => new Search<T>(isPhoneticSearch: true, phoneticKeyEncoder: BmpmPhoneticEncoder.EncodeApprox),
      _ => throw new InvalidOperationException(
          $"Неизвестный фонетический алгоритм: {mode}.")
    };
  }
}