namespace SearchEngine.Service;

/// <summary>
/// Хранит зарегистрированные provider-ы источников данных.
/// </summary>
public sealed class SearchDataSourceReaderRegistry
{
  private readonly Dictionary<string, ISearchDataSourceReader> _readers;

  /// <summary>
  /// Создаёт registry provider-ов источников данных.
  /// </summary>
  /// <param name="readers">Зарегистрированные reader-ы источников данных.</param>
  public SearchDataSourceReaderRegistry(IEnumerable<ISearchDataSourceReader> readers)
  {
    _readers = readers
        .GroupBy(reader => reader.Provider, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            group => group.Key,
            group => group.First(),
            StringComparer.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Возвращает список поддерживаемых provider-ов.
  /// </summary>
  /// <returns>Имена поддерживаемых provider-ов.</returns>
  public string[] GetSupportedProviders() => [.. _readers.Keys.OrderBy(provider => provider, StringComparer.OrdinalIgnoreCase)];

  /// <summary>
  /// Проверяет, поддерживается ли provider источника данных.
  /// </summary>
  /// <param name="provider">Имя provider-а.</param>
  /// <returns><see langword="true"/>, если provider поддерживается.</returns>
  public bool IsSupported(string provider) => !string.IsNullOrWhiteSpace(provider) && _readers.ContainsKey(provider);

  /// <summary>
  /// Возвращает reader для provider-а источника данных.
  /// </summary>
  /// <param name="provider">Имя provider-а.</param>
  /// <returns>Reader источника данных или <see langword="null"/>, если provider не поддерживается.</returns>
  public ISearchDataSourceReader? GetReader(string provider)
  {
    if (string.IsNullOrWhiteSpace(provider))
      return null;

    return _readers.TryGetValue(provider, out ISearchDataSourceReader? reader)
        ? reader
        : null;
  }
}