namespace SearchEngine;

/// <summary>
/// Кодирует русские имена в кириллической записи с приближённой обработкой гласных.
/// </summary>
internal static class BmpmRussianCyrillicApproxEncoder
{
  private const int _maxStackBufferLength = 256;

  /// <summary>
  /// Возвращает точный и приближённый фонетические ключи для русского имени.
  /// </summary>
  /// <param name="source">Исходное имя.</param>
  /// <returns>Набор фонетических ключей.</returns>
  public static IReadOnlyList<string> Encode(string? source)
  {
    IReadOnlyList<string> exactKeys = BmpmRussianCyrillicEncoder.Encode(source);

    if (exactKeys.Count == 0)
      return [];

    string exactKey = exactKeys[0];
    string approxKey = BuildApproxKey(exactKey);

    return string.Equals(exactKey, approxKey, StringComparison.Ordinal)
        ? exactKeys
        : [exactKey, approxKey];
  }

  /// <summary>
  /// Строит приближённый ключ на основе точного BMPM-ключа.
  /// </summary>
  /// <param name="source">Точный фонетический ключ.</param>
  /// <returns>Приближённый фонетический ключ.</returns>
  private static string BuildApproxKey(string source)
  {
    Span<char> buffer = source.Length <= _maxStackBufferLength
        ? stackalloc char[source.Length]
        : new char[source.Length];

    int position = 0;

    for (int i = 0, count = source.Length; i < count; i++)
    {
      char symbol = MapApproxSymbol(source[i]);

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
  /// Переводит символ точного ключа в приближённую фонетическую группу.
  /// </summary>
  /// <param name="symbol">Символ точного ключа.</param>
  /// <returns>Символ приближённого ключа.</returns>
  private static char MapApproxSymbol(char symbol)
  {
    return symbol switch
    {
      'О' => 'А',
      'Е' or 'Э' or 'Ы' => 'И',
      _ => symbol
    };
  }
}