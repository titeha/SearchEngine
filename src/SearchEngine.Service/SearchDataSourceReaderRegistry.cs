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
        .Where(reader => !string.IsNullOrWhiteSpace(reader.Provider))
        .GroupBy(reader => NormalizeProvider(reader.Provider)!, StringComparer.OrdinalIgnoreCase)
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
  public bool IsSupported(string provider)
  {
    string? normalizedProvider = NormalizeProvider(provider);

    return normalizedProvider is not null && _readers.ContainsKey(normalizedProvider);
  }

  /// <summary>
  /// Возвращает reader для provider-а источника данных.
  /// </summary>
  /// <param name="provider">Имя provider-а.</param>
  /// <returns>Reader источника данных или <see langword="null"/>, если provider не поддерживается.</returns>
  public ISearchDataSourceReader? GetReader(string provider)
  {
    string? normalizedProvider = NormalizeProvider(provider);
    if (normalizedProvider is null)
      return null;

    return _readers.TryGetValue(normalizedProvider, out ISearchDataSourceReader? reader)
        ? reader
        : null;
  }

  /// <summary>
  /// Нормализует имя provider-а для поиска в registry.
  /// </summary>
  /// <param name="provider">Имя provider-а.</param>
  /// <returns>Нормализованное имя provider-а или <see langword="null"/>, если имя пустое.</returns>
  private static string? NormalizeProvider(string? provider)
  {
    if (string.IsNullOrWhiteSpace(provider))
      return null;

    return provider.Trim();
  }
}