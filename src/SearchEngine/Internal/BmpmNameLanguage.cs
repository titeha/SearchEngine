namespace SearchEngine;

/// <summary>
/// Определяет язык и способ записи имени для BMPM-кодировщика.
/// </summary>
internal enum BmpmNameLanguage
{
  /// <summary>
  /// Язык определить не удалось.
  /// </summary>
  Unknown,

  /// <summary>
  /// Русское имя, записанное кириллицей.
  /// </summary>
  RussianCyrillic,

  /// <summary>
  /// Русское имя, записанное латиницей.
  /// </summary>
  RussianLatin
}