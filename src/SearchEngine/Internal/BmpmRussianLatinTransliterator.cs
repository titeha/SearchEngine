namespace SearchEngine;

/// <summary>
/// Переводит русские имена из латинской записи в кириллицу для BMPM-кодировщика.
/// </summary>
internal static class BmpmRussianLatinTransliterator
{
  private const int _maxStackBufferLength = 256;
  private const char _emptySymbol = '\0';

  /// <summary>
  /// Переводит исходную строку из латинской записи в кириллицу.
  /// </summary>
  /// <param name="source">Исходное имя в латинской записи.</param>
  /// <returns>Имя в кириллической записи или пустая строка, если входные данные не подходят.</returns>
  public static string Transliterate(string? source)
  {
    if (string.IsNullOrWhiteSpace(source))
      return string.Empty;

    ReadOnlySpan<char> value = source.AsSpan().Trim();

    int bufferLength = value.Length * 2;

    Span<char> buffer = bufferLength <= _maxStackBufferLength
        ? stackalloc char[bufferLength]
        : new char[bufferLength];

    int position = 0;

    for (int i = 0, count = value.Length; i < count; i++)
    {
      char symbol = char.ToUpperInvariant(value[i]);

      if (IsNameSeparator(symbol))
        continue;

      if (!IsLatinLetter(symbol))
        return string.Empty;

      if (TryAppendMultiLetterSymbol(value, ref i, buffer, ref position))
        continue;

      symbol = MapSingleLetter(symbol);

      if (symbol == _emptySymbol)
        return string.Empty;

      AppendSymbol(buffer, ref position, symbol);
    }

    return position == 0
        ? string.Empty
        : new string(buffer[..position]);
  }

  /// <summary>
  /// Пробует добавить кириллический символ для многобуквенного латинского сочетания.
  /// </summary>
  /// <param name="source">Исходная строка.</param>
  /// <param name="index">Текущая позиция чтения.</param>
  /// <param name="buffer">Буфер результата.</param>
  /// <param name="position">Текущая позиция записи.</param>
  /// <returns>
  /// <see langword="true"/>, если сочетание найдено; иначе <see langword="false"/>.
  /// </returns>
  private static bool TryAppendMultiLetterSymbol(
      ReadOnlySpan<char> source,
      ref int index,
      Span<char> buffer,
      ref int position)
  {
    if (TryMatch(source, index, "SHCH") || TryMatch(source, index, "SCH"))
    {
      AppendSymbol(buffer, ref position, 'Щ');
      index += TryMatch(source, index, "SHCH") ? 3 : 2;

      return true;
    }

    if (TryMatch(source, index, "DZH"))
    {
      AppendSymbol(buffer, ref position, 'Д');
      AppendSymbol(buffer, ref position, 'Ж');
      index += 2;

      return true;
    }

    if (TryMatch(source, index, "TCH"))
    {
      AppendSymbol(buffer, ref position, 'Ч');
      index += 2;

      return true;
    }

    if (TryMatch(source, index, "ZH"))
    {
      AppendSymbol(buffer, ref position, 'Ж');
      index++;

      return true;
    }

    if (TryMatch(source, index, "KH"))
    {
      AppendSymbol(buffer, ref position, 'Х');
      index++;

      return true;
    }

    if (TryMatch(source, index, "TS"))
    {
      AppendSymbol(buffer, ref position, 'Ц');
      index++;

      return true;
    }

    if (TryMatch(source, index, "CH"))
    {
      AppendSymbol(buffer, ref position, 'Ч');
      index++;

      return true;
    }

    if (TryMatch(source, index, "SH"))
    {
      AppendSymbol(buffer, ref position, 'Ш');
      index++;

      return true;
    }

    if (TryMatch(source, index, "YU") || TryMatch(source, index, "IU"))
    {
      AppendSymbol(buffer, ref position, 'Ю');
      index++;

      return true;
    }

    if (TryMatch(source, index, "YA") || TryMatch(source, index, "IA"))
    {
      AppendSymbol(buffer, ref position, 'Я');
      index++;

      return true;
    }

    if (TryMatch(source, index, "YO")
        || TryMatch(source, index, "JO")
        || TryMatch(source, index, "YE"))
    {
      AppendSymbol(buffer, ref position, 'Е');
      index++;

      return true;
    }

    return false;
  }

  /// <summary>
  /// Проверяет, начинается ли строка с указанного латинского сочетания.
  /// </summary>
  /// <param name="source">Исходная строка.</param>
  /// <param name="index">Позиция начала проверки.</param>
  /// <param name="pattern">Проверяемое сочетание.</param>
  /// <returns>
  /// <see langword="true"/>, если сочетание найдено; иначе <see langword="false"/>.
  /// </returns>
  private static bool TryMatch(
      ReadOnlySpan<char> source,
      int index,
      string pattern)
  {
    if (index + pattern.Length > source.Length)
      return false;

    for (int i = 0, count = pattern.Length; i < count; i++)
      if (char.ToUpperInvariant(source[index + i]) != pattern[i])
        return false;

    return true;
  }

  /// <summary>
  /// Переводит одиночную латинскую букву в кириллическую.
  /// </summary>
  /// <param name="symbol">Латинская буква.</param>
  /// <returns>Кириллическая буква или пустой символ, если буква не поддерживается.</returns>
  private static char MapSingleLetter(char symbol)
  {
    return symbol switch
    {
      'A' => 'А',
      'B' => 'Б',
      'C' => 'К',
      'D' => 'Д',
      'E' => 'Е',
      'F' => 'Ф',
      'G' => 'Г',
      'H' => 'Х',
      'I' => 'И',
      'J' => 'Й',
      'K' => 'К',
      'L' => 'Л',
      'M' => 'М',
      'N' => 'Н',
      'O' => 'О',
      'P' => 'П',
      'Q' => 'К',
      'R' => 'Р',
      'S' => 'С',
      'T' => 'Т',
      'U' => 'У',
      'V' => 'В',
      'W' => 'В',
      'Y' => 'И',
      'Z' => 'З',
      _ => _emptySymbol
    };
  }

  /// <summary>
  /// Добавляет символ в буфер результата.
  /// </summary>
  /// <param name="buffer">Буфер результата.</param>
  /// <param name="position">Текущая позиция записи.</param>
  /// <param name="symbol">Добавляемый символ.</param>
  private static void AppendSymbol(
      Span<char> buffer,
      ref int position,
      char symbol)
  {
    buffer[position] = symbol;
    position++;
  }

  /// <summary>
  /// Проверяет, является ли символ латинской буквой.
  /// </summary>
  /// <param name="symbol">Проверяемый символ.</param>
  /// <returns>
  /// <see langword="true"/>, если символ является латинской буквой; иначе <see langword="false"/>.
  /// </returns>
  private static bool IsLatinLetter(char symbol) => symbol is >= 'A' and <= 'Z';

  /// <summary>
  /// Проверяет, является ли символ разделителем внутри имени.
  /// </summary>
  /// <param name="symbol">Проверяемый символ.</param>
  /// <returns>
  /// <see langword="true"/>, если символ является разделителем; иначе <see langword="false"/>.
  /// </returns>
  private static bool IsNameSeparator(char symbol) => char.IsWhiteSpace(symbol) || symbol is '-' or '‑' or '–' or '—' or '\'' or '’';
}