namespace SearchEngine;

/// <summary>
/// Определяет язык имени для BMPM-кодировщика.
/// </summary>
internal static class BmpmNameLanguageDetector
{
  private static readonly string[] _russianLatinMarkers =
  [
      "SHCH",
        "SCH",
        "ZH",
        "KH",
        "TS",
        "CH",
        "SH",
        "YU",
        "YA",
        "YO",
        "YE"
  ];

  private static readonly string[] _russianLatinEndings =
  [
      "OV",
        "EV",
        "IN",
        "YN",
        "OVA",
        "EVA",
        "INA",
        "YNA",
        "SKY",
        "SKIY",
        "SKAYA",
        "SKAIA",
        "TSKY",
        "TSKIY"
  ];

  /// <summary>
  /// Определяет язык и способ записи имени.
  /// </summary>
  /// <param name="source">Исходное имя.</param>
  /// <returns>Определённый язык имени.</returns>
  public static BmpmNameLanguage Detect(string? source)
  {
    if (string.IsNullOrWhiteSpace(source))
      return BmpmNameLanguage.Unknown;

    bool hasCyrillic = false;
    bool hasLatin = false;

    ReadOnlySpan<char> value = source.AsSpan().Trim();

    for (int i = 0, count = value.Length; i < count; i++)
    {
      char symbol = char.ToUpperInvariant(value[i]);

      if (IsRussianCyrillicLetter(symbol))
        hasCyrillic = true;
      else if (IsLatinLetter(symbol))
        hasLatin = true;
      else if (!IsNameSeparator(symbol))
        return BmpmNameLanguage.Unknown;

      if (hasCyrillic && hasLatin)
        return BmpmNameLanguage.Unknown;
    }

    if (hasCyrillic)
      return BmpmNameLanguage.RussianCyrillic;

    if (!hasLatin)
      return BmpmNameLanguage.Unknown;

    return IsRussianLatinName(source)
        ? BmpmNameLanguage.RussianLatin
        : BmpmNameLanguage.Unknown;
  }

  /// <summary>
  /// Проверяет, является ли символ русской кириллической буквой.
  /// </summary>
  /// <param name="symbol">Проверяемый символ.</param>
  /// <returns>
  /// <see langword="true"/>, если символ является русской кириллической буквой;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool IsRussianCyrillicLetter(char symbol) => symbol is >= 'А' and <= 'Я' or 'Ё';

  /// <summary>
  /// Проверяет, является ли символ латинской буквой.
  /// </summary>
  /// <param name="symbol">Проверяемый символ.</param>
  /// <returns>
  /// <see langword="true"/>, если символ является латинской буквой;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool IsLatinLetter(char symbol) => symbol is >= 'A' and <= 'Z';

  /// <summary>
  /// Проверяет, является ли символ допустимым разделителем внутри имени.
  /// </summary>
  /// <param name="symbol">Проверяемый символ.</param>
  /// <returns>
  /// <see langword="true"/>, если символ является допустимым разделителем;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool IsNameSeparator(char symbol) => char.IsWhiteSpace(symbol) || symbol is '-' or '\'' or '’';

  /// <summary>
  /// Проверяет, похоже ли имя в латинской записи на русскую транслитерацию.
  /// </summary>
  /// <param name="source">Исходное имя.</param>
  /// <returns>
  /// <see langword="true"/>, если имя похоже на русскую транслитерацию;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool IsRussianLatinName(string source)
  {
    string normalized = source.Trim().ToUpperInvariant();

    return HasRussianLatinMarker(normalized)
           || HasRussianLatinEnding(normalized);
  }

  /// <summary>
  /// Проверяет наличие характерных сочетаний русской транслитерации.
  /// </summary>
  /// <param name="source">Нормализованное имя.</param>
  /// <returns>
  /// <see langword="true"/>, если найдено характерное сочетание;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool HasRussianLatinMarker(string source)
  {
    for (int i = 0, count = _russianLatinMarkers.Length; i < count; i++)
      if (source.Contains(_russianLatinMarkers[i], StringComparison.Ordinal))
        return true;

    return false;
  }

  /// <summary>
  /// Проверяет наличие характерного окончания русской фамилии в латинской записи.
  /// </summary>
  /// <param name="source">Нормализованное имя.</param>
  /// <returns>
  /// <see langword="true"/>, если найдено характерное окончание;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool HasRussianLatinEnding(string source)
  {
    for (int i = 0, count = _russianLatinEndings.Length; i < count; i++)
      if (source.EndsWith(_russianLatinEndings[i], StringComparison.Ordinal))
        return true;

    return false;
  }
}