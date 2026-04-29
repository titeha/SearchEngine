namespace SearchEngine;

/// <summary>
/// Содержит настройку фонетического кодировщика для поискового движка.
/// </summary>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Кодировщик, используемый для получения фонетических ключей.
  /// </summary>
  private readonly Func<string, IReadOnlyList<string>> _phoneticEncoder;

  /// <summary>
  /// Создаёт экземпляр поискового движка и позволяет подменить фонетический кодировщик.
  /// </summary>
  /// <param name="isPhoneticSearch">
  /// <see langword="true"/>, если нужно использовать фонетический поиск.
  /// </param>
  /// <param name="phoneticEncoder">
  /// Функция получения одного фонетического ключа.
  /// Если значение не задано, используется текущий встроенный кодировщик.
  /// </param>
  internal Search(bool isPhoneticSearch, Func<string, string>? phoneticEncoder) : this()
  {
    IsPhoneticSearch = isPhoneticSearch;
    _phoneticEncoder = phoneticEncoder is null
        ? EncodeDefaultPhoneticKeys
        : source => EncodeSinglePhoneticKey(phoneticEncoder, source);
  }

  /// <summary>
  /// Создаёт экземпляр поискового движка и позволяет подменить фонетический кодировщик
  /// на вариант, возвращающий несколько фонетических ключей.
  /// </summary>
  /// <param name="isPhoneticSearch">
  /// <see langword="true"/>, если нужно использовать фонетический поиск.
  /// </param>
  /// <param name="phoneticKeyEncoder">
  /// Функция получения набора фонетических ключей.
  /// Если значение не задано, используется текущий встроенный кодировщик.
  /// </param>
  internal Search(bool isPhoneticSearch, Func<string, IReadOnlyList<string>>? phoneticKeyEncoder) : this()
  {
    IsPhoneticSearch = isPhoneticSearch;
    _phoneticEncoder = phoneticKeyEncoder ?? EncodeDefaultPhoneticKeys;
  }

  /// <summary>
  /// Получает первый фонетический ключ для исходной строки.
  /// </summary>
  /// <param name="source">Исходная строка.</param>
  /// <returns>Первый фонетический ключ.</returns>
  internal string EncodePhonetic(string source)
  {
    IReadOnlyList<string> keys = EncodePhoneticKeys(source);

    return keys.Count == 0
        ? string.Empty
        : keys[0];
  }

  /// <summary>
  /// Возвращает фонетические ключи для исходной строки.
  /// </summary>
  /// <param name="source">Исходная строка.</param>
  /// <returns>Набор фонетических ключей.</returns>
  internal IReadOnlyList<string> EncodePhoneticKeys(string source)
  {
    if (string.IsNullOrWhiteSpace(source))
      return [];

    return NormalizePhoneticKeys(_phoneticEncoder(source));
  }

  /// <summary>
  /// Возвращает фонетические ключи встроенного кодировщика.
  /// </summary>
  /// <param name="source">Исходная строка.</param>
  /// <returns>Набор фонетических ключей.</returns>
  private static IReadOnlyList<string> EncodeDefaultPhoneticKeys(string source) => BmpmPhoneticEncoder.EncodeApprox(source);

  /// <summary>
  /// Оборачивает кодировщик одного ключа в кодировщик набора ключей.
  /// </summary>
  /// <param name="phoneticEncoder">Кодировщик одного фонетического ключа.</param>
  /// <param name="source">Исходная строка.</param>
  /// <returns>Набор фонетических ключей.</returns>
  private static IReadOnlyList<string> EncodeSinglePhoneticKey(
      Func<string, string> phoneticEncoder,
      string source)
  {
    string key = phoneticEncoder(source);

    return string.IsNullOrWhiteSpace(key)
        ? []
        : [key];
  }

  /// <summary>
  /// Удаляет пустые и повторяющиеся фонетические ключи.
  /// </summary>
  /// <param name="keys">Исходный набор фонетических ключей.</param>
  /// <returns>Очищенный набор фонетических ключей.</returns>
  private static IReadOnlyList<string> NormalizePhoneticKeys(IReadOnlyList<string>? keys)
  {
    if (keys is null || keys.Count == 0)
      return [];

    if (keys.Count == 1)
    {
      string key = keys[0];

      return string.IsNullOrWhiteSpace(key)
          ? []
          : keys;
    }

    List<string> result = new(keys.Count);

    for (int i = 0, count = keys.Count; i < count; i++)
    {
      string key = keys[i];

      if (string.IsNullOrWhiteSpace(key))
        continue;

      if (!ContainsPhoneticKey(result, key))
        result.Add(key);
    }

    return result.Count == 0
        ? []
        : result;
  }

  /// <summary>
  /// Проверяет наличие фонетического ключа в уже подготовленном наборе.
  /// </summary>
  /// <param name="keys">Подготовленный набор ключей.</param>
  /// <param name="key">Проверяемый ключ.</param>
  /// <returns>
  /// <see langword="true"/>, если ключ уже есть в наборе; иначе <see langword="false"/>.
  /// </returns>
  private static bool ContainsPhoneticKey(List<string> keys, string key)
  {
    for (int i = 0, count = keys.Count; i < count; i++)
      if (string.Equals(keys[i], key, StringComparison.Ordinal))
        return true;

    return false;
  }
}