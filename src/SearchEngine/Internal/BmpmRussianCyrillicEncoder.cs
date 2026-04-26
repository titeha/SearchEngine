namespace SearchEngine;

/// <summary>
/// Кодирует русские имена в кириллической записи для BMPM-кодировщика.
/// </summary>
internal static class BmpmRussianCyrillicEncoder
{
  private const int _maxStackBufferLength = 256;

  /// <summary>
  /// Возвращает фонетические ключи для русского имени в кириллической записи.
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
  /// Кодирует уже нормализованное имя.
  /// </summary>
  /// <param name="source">Нормализованное имя.</param>
  /// <returns>Фонетический ключ.</returns>
  private static string EncodeNormalized(string source)
  {
    Span<char> buffer = source.Length <= _maxStackBufferLength
        ? stackalloc char[source.Length]
        : new char[source.Length];

    int position = 0;

    for (int i = 0, count = source.Length; i < count; i++)
    {
      char symbol = MapSymbol(source[i]);
      position = AppendSymbol(buffer, position, symbol);
    }

    return position == 0
        ? string.Empty
        : new string(buffer[..position]);
  }

  /// <summary>
  /// Добавляет символ в буфер и сжимает соседние повторы.
  /// </summary>
  /// <param name="buffer">Буфер фонетического ключа.</param>
  /// <param name="position">Текущая позиция записи.</param>
  /// <param name="symbol">Добавляемый символ.</param>
  /// <returns>Новая позиция записи.</returns>
  private static int AppendSymbol(Span<char> buffer, int position, char symbol)
  {
    if (position > 0 && buffer[position - 1] == symbol)
      return position;

    buffer[position] = symbol;

    return position + 1;
  }

  /// <summary>
  /// Переводит букву в базовую фонетическую группу.
  /// </summary>
  /// <param name="symbol">Исходная буква.</param>
  /// <returns>Код фонетической группы.</returns>
  private static char MapSymbol(char symbol)
  {
    return symbol switch
    {
      'Б' or 'П' => 'П',
      'В' or 'Ф' => 'Ф',
      'Г' or 'К' or 'Х' => 'К',
      'Д' or 'Т' => 'Т',
      'Ж' or 'Ш' or 'Щ' or 'Ч' => 'Ш',
      'З' or 'С' or 'Ц' => 'С',
      'Й' or 'И' or 'Ы' => 'И',
      'Ю' or 'У' => 'У',
      'Я' or 'А' => 'А',
      'Э' or 'Е' => 'Е',
      _ => symbol
    };
  }
}