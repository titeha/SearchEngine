namespace SearchEngine;

/// <summary>
/// Кодирует русские имена в кириллической записи с приближённой обработкой гласных.
/// </summary>
internal static class BmpmRussianCyrillicApproxEncoder
{
  private const int MaxStackBufferLength = 256;

  /// <summary>
  /// Возвращает приближённый фонетический ключ для русского имени.
  /// </summary>
  /// <param name="source">Исходное имя.</param>
  /// <returns>Набор фонетических ключей.</returns>
  public static IReadOnlyList<string> Encode(string? source)
  {
    string normalized = BmpmRussianCyrillicNormalizer.Normalize(source);

    if (normalized.Length == 0)
      return [];

    string key = EncodeNormalized(normalized);

    return key.Length == 0
        ? []
        : [key];
  }

  /// <summary>
  /// Кодирует уже нормализованное имя в приближённый фонетический ключ.
  /// </summary>
  /// <param name="source">Нормализованное имя.</param>
  /// <returns>Приближённый фонетический ключ.</returns>
  private static string EncodeNormalized(string source)
  {
    Span<char> buffer = source.Length <= MaxStackBufferLength
        ? stackalloc char[source.Length]
        : new char[source.Length];

    int position = 0;

    for (int i = 0, count = source.Length; i < count; i++)
    {
      char symbol = BmpmRussianCyrillicEncoder.MapSymbol(source[i]);
      symbol = MapApproxSymbol(symbol);

      if (position > 0 && buffer[position - 1] == symbol)
        continue;

      buffer[position] = symbol;
      position++;
    }

    return position == 0
        ? string.Empty
        : new string(buffer[..position]);
  }

  /// <summary>
  /// Переводит символ базового ключа в приближённую фонетическую группу.
  /// </summary>
  /// <param name="symbol">Символ базового ключа.</param>
  /// <returns>Символ приближённого ключа.</returns>
  private static char MapApproxSymbol(char symbol) => symbol switch
  {
    'О' => 'А',
    'Е' or 'Э' or 'Ы' => 'И',
    _ => symbol
  };
}