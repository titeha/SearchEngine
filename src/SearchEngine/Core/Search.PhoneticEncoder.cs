namespace SearchEngine;

/// <summary>
/// Содержит настройку фонетического кодировщика для поискового движка.
/// </summary>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Кодировщик, используемый для получения фонетического ключа.
  /// </summary>
  private readonly Func<string, string> _phoneticEncoder;

  /// <summary>
  /// Создаёт экземпляр поискового движка и позволяет подменить фонетический кодировщик.
  /// </summary>
  /// <param name="isPhoneticSearch">
  /// <see langword="true"/>, если нужно использовать фонетический поиск.
  /// </param>
  /// <param name="phoneticEncoder">
  /// Функция получения фонетического ключа.
  /// Если значение не задано, используется текущий встроенный кодировщик.
  /// </param>
  internal Search(bool isPhoneticSearch, Func<string, string>? phoneticEncoder) : this()
  {
    IsPhoneticSearch = isPhoneticSearch;
    _phoneticEncoder = phoneticEncoder ?? PhoneticSearch.MetaPhone;
  }

  /// <summary>
  /// Получает фонетический ключ для исходной строки.
  /// </summary>
  /// <param name="source">Исходная строка.</param>
  /// <returns>Фонетический ключ.</returns>
  internal string EncodePhonetic(string source)
  {
    if (string.IsNullOrWhiteSpace(source))
      return string.Empty;

    return _phoneticEncoder(source);
  }

  /// <summary>
  /// Возвращает фонетические ключи для исходной строки.
  /// </summary>
  /// <param name="source">Исходная строка.</param>
  /// <returns>Набор фонетических ключей.</returns>
  internal IReadOnlyList<string> EncodePhoneticKeys(string source)
  {
    string key = EncodePhonetic(source);

    if (string.IsNullOrWhiteSpace(key))
      return [];

    return [key];
  }
}