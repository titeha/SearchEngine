namespace SearchEngine;

/// <summary>
/// Кодирует русские имена в латинской записи для BMPM-кодировщика.
/// </summary>
internal static class BmpmRussianLatinEncoder
{
  /// <summary>
  /// Возвращает фонетические ключи для русского имени в латинской записи.
  /// </summary>
  /// <param name="source">Исходное имя в латинской записи.</param>
  /// <returns>Набор фонетических ключей.</returns>
  public static IReadOnlyList<string> Encode(string? source)
  {
    string cyrillic = BmpmRussianLatinTransliterator.Transliterate(source);

    return cyrillic.Length == 0
        ? []
        : BmpmRussianCyrillicEncoder.Encode(cyrillic);
  }

  /// <summary>
  /// Возвращает приближённые фонетические ключи для русского имени в латинской записи.
  /// </summary>
  /// <param name="source">Исходное имя в латинской записи.</param>
  /// <returns>Набор фонетических ключей.</returns>
  public static IReadOnlyList<string> EncodeApprox(string? source)
  {
    string cyrillic = BmpmRussianLatinTransliterator.Transliterate(source);

    return cyrillic.Length == 0
        ? []
        : BmpmRussianCyrillicApproxEncoder.Encode(cyrillic);
  }
}