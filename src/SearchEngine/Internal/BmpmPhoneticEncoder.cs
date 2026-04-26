namespace SearchEngine;

/// <summary>
/// Кодирует имена по правилам BMPM.
/// </summary>
internal static class BmpmPhoneticEncoder
{
  /// <summary>
  /// Возвращает фонетические ключи для исходного имени.
  /// </summary>
  /// <param name="source">Исходное имя.</param>
  /// <returns>Набор фонетических ключей.</returns>
  public static IReadOnlyList<string> Encode(string? source)
  {
    BmpmNameLanguage language = BmpmNameLanguageDetector.Detect(source);

    return language switch
    {
      BmpmNameLanguage.RussianCyrillic => BmpmRussianCyrillicEncoder.Encode(source),
      BmpmNameLanguage.RussianLatin => [],
      _ => []
    };
  }
}