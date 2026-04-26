using System.Text;

namespace SearchEngine.Benchmarks;

/// <summary>
/// Печатает диагностический отчёт по распределению фонетических ключей.
/// </summary>
internal static class PhoneticKeyDistributionDiagnostic
{
  private const int _maxSampleCount = 10;
  private const int _maxLargestBucketCount = 15;

  private static readonly string[] _targetSurnames =
  [
      "Петров",
        "Смирнов",
        "Забабанова",
        "Забабонова",
        "Папандопуло",
        "Папондопуло",
        "Гоголадзе",
        "Коколадзе",
        "Иванов",
        "Семёнов"
  ];

  /// <summary>
  /// Выполняет диагностику распределения фонетических ключей.
  /// </summary>
  public static void Run()
  {
    string[] surnames = RealSurnameDataLoader.LoadSurnames();

    Console.WriteLine("Диагностика распределения фонетических ключей");
    Console.WriteLine("--------------------------------------------");
    Console.WriteLine($"Фамилий в наборе: {surnames.Length:N0}");
    Console.WriteLine();

    PrintReport(
        PhoneticAlgorithmBenchMode.MetaPhone,
        surnames,
        EncodeMetaPhone);

    Console.WriteLine();

    PrintReport(
        PhoneticAlgorithmBenchMode.Bmpm,
        surnames,
        BmpmPhoneticEncoder.Encode);
  }

  /// <summary>
  /// Печатает отчёт по одному фонетическому алгоритму.
  /// </summary>
  /// <param name="algorithm">Фонетический алгоритм.</param>
  /// <param name="surnames">Фамилии для проверки.</param>
  /// <param name="encoder">Кодировщик фонетических ключей.</param>
  private static void PrintReport(
      PhoneticAlgorithmBenchMode algorithm,
      string[] surnames,
      Func<string, IReadOnlyList<string>> encoder)
  {
    Dictionary<string, List<string>> buckets = BuildBuckets(
        surnames,
        encoder,
        out int encodedSurnameCount,
        out int skippedSurnameCount,
        out int totalKeyCount);

    PrintSummary(
        algorithm,
        surnames.Length,
        encodedSurnameCount,
        skippedSurnameCount,
        totalKeyCount,
        buckets);

    PrintLargestBuckets(buckets);
    PrintTargetBuckets(encoder, buckets);
  }

  /// <summary>
  /// Строит распределение фамилий по фонетическим ключам.
  /// </summary>
  /// <param name="surnames">Фамилии для проверки.</param>
  /// <param name="encoder">Кодировщик фонетических ключей.</param>
  /// <param name="encodedSurnameCount">Количество фамилий, для которых получен хотя бы один ключ.</param>
  /// <param name="skippedSurnameCount">Количество фамилий без фонетических ключей.</param>
  /// <param name="totalKeyCount">Общее количество связей фамилия-ключ.</param>
  /// <returns>Словарь фонетических ключей и соответствующих фамилий.</returns>
  private static Dictionary<string, List<string>> BuildBuckets(
      string[] surnames,
      Func<string, IReadOnlyList<string>> encoder,
      out int encodedSurnameCount,
      out int skippedSurnameCount,
      out int totalKeyCount)
  {
    Dictionary<string, List<string>> buckets = new(StringComparer.Ordinal);

    encodedSurnameCount = 0;
    skippedSurnameCount = 0;
    totalKeyCount = 0;

    for (int i = 0, count = surnames.Length; i < count; i++)
    {
      string surname = surnames[i];
      IReadOnlyList<string> keys = encoder(surname);

      if (keys.Count == 0)
      {
        skippedSurnameCount++;
        continue;
      }

      HashSet<string>? surnameKeys = keys.Count > 1
          ? new HashSet<string>(StringComparer.Ordinal)
          : null;

      bool hasKey = false;

      for (int keyIndex = 0, keyCount = keys.Count; keyIndex < keyCount; keyIndex++)
      {
        string key = keys[keyIndex];

        if (string.IsNullOrWhiteSpace(key))
          continue;

        if (surnameKeys is not null && !surnameKeys.Add(key))
          continue;

        if (!buckets.TryGetValue(key, out List<string>? bucket))
        {
          bucket = [];
          buckets.Add(key, bucket);
        }

        bucket.Add(surname);
        totalKeyCount++;
        hasKey = true;
      }

      if (hasKey)
        encodedSurnameCount++;
      else
        skippedSurnameCount++;
    }

    return buckets;
  }

  /// <summary>
  /// Печатает сводные показатели распределения.
  /// </summary>
  /// <param name="algorithm">Фонетический алгоритм.</param>
  /// <param name="sourceCount">Количество исходных фамилий.</param>
  /// <param name="encodedSurnameCount">Количество закодированных фамилий.</param>
  /// <param name="skippedSurnameCount">Количество пропущенных фамилий.</param>
  /// <param name="totalKeyCount">Общее количество связей фамилия-ключ.</param>
  /// <param name="buckets">Словарь фонетических ключей и соответствующих фамилий.</param>
  private static void PrintSummary(
      PhoneticAlgorithmBenchMode algorithm,
      int sourceCount,
      int encodedSurnameCount,
      int skippedSurnameCount,
      int totalKeyCount,
      Dictionary<string, List<string>> buckets)
  {
    int maxBucketSize = 0;
    int bucketsMoreThanOne = 0;
    int bucketsMoreThanFive = 0;
    int bucketsMoreThanTen = 0;

    foreach (List<string> bucket in buckets.Values)
    {
      int size = bucket.Count;

      if (size > maxBucketSize)
        maxBucketSize = size;

      if (size > 1)
        bucketsMoreThanOne++;

      if (size > 5)
        bucketsMoreThanFive++;

      if (size > 10)
        bucketsMoreThanTen++;
    }

    double averageBucketSize = buckets.Count == 0
        ? 0
        : (double)totalKeyCount / buckets.Count;

    double compressionRatio = sourceCount == 0
        ? 0
        : (double)buckets.Count / sourceCount;

    Console.WriteLine($"Алгоритм: {algorithm}");
    Console.WriteLine($"  Исходных фамилий:             {sourceCount:N0}");
    Console.WriteLine($"  Закодировано фамилий:         {encodedSurnameCount:N0}");
    Console.WriteLine($"  Пропущено фамилий:            {skippedSurnameCount:N0}");
    Console.WriteLine($"  Уникальных ключей:            {buckets.Count:N0}");
    Console.WriteLine($"  Связей фамилия-ключ:          {totalKeyCount:N0}");
    Console.WriteLine($"  Средний размер bucket-а:      {averageBucketSize:N2}");
    Console.WriteLine($"  Максимальный размер bucket-а: {maxBucketSize:N0}");
    Console.WriteLine($"  Bucket-ов размером > 1:       {bucketsMoreThanOne:N0}");
    Console.WriteLine($"  Bucket-ов размером > 5:       {bucketsMoreThanFive:N0}");
    Console.WriteLine($"  Bucket-ов размером > 10:      {bucketsMoreThanTen:N0}");
    Console.WriteLine($"  Доля уникальных ключей:       {compressionRatio:N4}");
    Console.WriteLine();
  }

  /// <summary>
  /// Печатает самые широкие bucket-ы.
  /// </summary>
  /// <param name="buckets">Словарь фонетических ключей и соответствующих фамилий.</param>
  private static void PrintLargestBuckets(Dictionary<string, List<string>> buckets)
  {
    Console.WriteLine("  Самые широкие bucket-ы:");

    int position = 0;

    foreach (var bucket in buckets
        .OrderByDescending(static item => item.Value.Count)
        .ThenBy(static item => item.Key, StringComparer.Ordinal)
        .Take(_maxLargestBucketCount))
    {
      position++;

      Console.WriteLine(
          $"    {position,2}. {bucket.Key}: {bucket.Value.Count:N0} -> " +
          FormatSample(bucket.Value, _maxSampleCount));
    }

    Console.WriteLine();
  }

  /// <summary>
  /// Печатает bucket-ы для целевых фамилий.
  /// </summary>
  /// <param name="encoder">Кодировщик фонетических ключей.</param>
  /// <param name="buckets">Словарь фонетических ключей и соответствующих фамилий.</param>
  private static void PrintTargetBuckets(
      Func<string, IReadOnlyList<string>> encoder,
      Dictionary<string, List<string>> buckets)
  {
    Console.WriteLine("  Целевые фамилии:");

    for (int i = 0, count = _targetSurnames.Length; i < count; i++)
    {
      string surname = _targetSurnames[i];
      IReadOnlyList<string> keys = encoder(surname);

      Console.WriteLine($"    {surname}: {FormatKeys(keys)}");

      for (int keyIndex = 0, keyCount = keys.Count; keyIndex < keyCount; keyIndex++)
      {
        string key = keys[keyIndex];

        if (string.IsNullOrWhiteSpace(key))
          continue;

        if (!buckets.TryGetValue(key, out List<string>? bucket))
        {
          Console.WriteLine($"      {key}: bucket не найден");
          continue;
        }

        Console.WriteLine(
            $"      {key}: {bucket.Count:N0} -> " +
            FormatSample(bucket, _maxSampleCount));
      }
    }

    Console.WriteLine();
  }

  /// <summary>
  /// Кодирует фамилию текущей MetaPhone-реализацией.
  /// </summary>
  /// <param name="source">Исходная фамилия.</param>
  /// <returns>Набор фонетических ключей.</returns>
  private static IReadOnlyList<string> EncodeMetaPhone(string source)
  {
    string key = Search<int>.PhoneticSearch.MetaPhone(source);

    return string.IsNullOrWhiteSpace(key)
        ? []
        : [key];
  }

  /// <summary>
  /// Форматирует набор фонетических ключей для вывода.
  /// </summary>
  /// <param name="keys">Фонетические ключи.</param>
  /// <returns>Строка для вывода.</returns>
  private static string FormatKeys(IReadOnlyList<string> keys)
  {
    if (keys.Count == 0)
      return "(ключей нет)";

    StringBuilder builder = new();

    for (int i = 0, count = keys.Count; i < count; i++)
    {
      if (i > 0)
        builder.Append(", ");

      builder.Append(keys[i]);
    }

    return builder.ToString();
  }

  /// <summary>
  /// Форматирует пример фамилий из bucket-а.
  /// </summary>
  /// <param name="surnames">Фамилии в bucket-е.</param>
  /// <param name="maxCount">Максимальное количество фамилий для вывода.</param>
  /// <returns>Строка для вывода.</returns>
  private static string FormatSample(IReadOnlyList<string> surnames, int maxCount)
  {
    if (surnames.Count == 0)
      return "(пусто)";

    int count = Math.Min(surnames.Count, maxCount);
    StringBuilder builder = new();

    for (int i = 0; i < count; i++)
    {
      if (i > 0)
        builder.Append(", ");

      builder.Append(surnames[i]);
    }

    if (surnames.Count > maxCount)
      builder.Append(", ...");

    return builder.ToString();
  }
}