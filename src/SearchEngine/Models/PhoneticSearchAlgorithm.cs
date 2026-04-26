namespace SearchEngine;

/// <summary>
/// Определяет фонетический алгоритм для фонетического поиска.
/// </summary>
public enum PhoneticSearchAlgorithm
{
  /// <summary>
  /// Текущая реализация MetaPhone.
  /// </summary>
  MetaPhone,

  /// <summary>
  /// BMPM-кодировщик для русских имён и фамилий в кириллической записи.
  /// </summary>
  RussianBmpm,

  /// <summary>
  /// BMPM-кодировщик для русских имён и фамилий в кириллической записи
  /// с приближённой обработкой гласных.
  /// </summary>
  RussianBmpmApprox
}