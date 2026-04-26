using StringFunctions;

namespace SearchEngine;

/// <summary>
/// Нормализует русские имена в кириллической записи для BMPM-кодировщика.
/// </summary>
internal static class BmpmRussianCyrillicNormalizer
{
  private const int _maxStackBufferLength = 256;
  private const char _emptySymbol = '\0';

  /// <summary>
  /// Нормализует исходную строку для дальнейшего фонетического кодирования.
  /// </summary>
  /// <param name="source">Исходная строка.</param>
  /// <returns>Нормализованная строка или пустая строка, если входные данные не подходят.</returns>
  public static string Normalize(string? source)
  {
    if (source.IsNullOrWhiteSpace())
      return string.Empty;

    Span<char> buffer = source!.Length <= _maxStackBufferLength
        ? stackalloc char[source.Length]
        : new char[source.Length];

    int position = 0;

    for (int i = 0, count = source.Length; i < count; i++)
    {
      char symbol = NormalizeSymbol(source[i]);

      if (symbol == _emptySymbol)
        continue;

      if (!IsRussianCyrillicLetter(symbol))
        return string.Empty;

      buffer[position] = symbol;
      position++;
    }

    return position == 0
        ? string.Empty
        : new string(buffer[..position]);
  }

  /// <summary>
  /// Нормализует отдельный символ.
  /// </summary>
  /// <param name="source">Исходный символ.</param>
  /// <returns>Нормализованный символ или пустой символ, если символ нужно пропустить.</returns>
  private static char NormalizeSymbol(char source)
  {
    if (IsNameSeparator(source))
      return _emptySymbol;

    char symbol = char.ToUpperInvariant(source);

    return symbol switch
    {
      'Ё' => 'Е',
      'Ь' or 'Ъ' => _emptySymbol,
      _ => symbol
    };
  }

  /// <summary>
  /// Проверяет, является ли символ русской кириллической буквой.
  /// </summary>
  /// <param name="symbol">Проверяемый символ.</param>
  /// <returns>
  /// <see langword="true"/>, если символ является русской кириллической буквой;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool IsRussianCyrillicLetter(char symbol) => symbol is >= 'А' and <= 'Я';

  /// <summary>
  /// Проверяет, является ли символ разделителем внутри имени.
  /// </summary>
  /// <param name="symbol">Проверяемый символ.</param>
  /// <returns>
  /// <see langword="true"/>, если символ является разделителем;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool IsNameSeparator(char symbol) => char.IsWhiteSpace(symbol) || symbol is '-' or '-' or '–' or '—' or '\'' or '’';
}