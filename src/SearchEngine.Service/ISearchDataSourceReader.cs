namespace SearchEngine.Service;

/// <summary>
/// Читает документы из заранее настроенного источника данных.
/// </summary>
public interface ISearchDataSourceReader
{
  /// <summary>
  /// Получает имя provider-а источника данных.
  /// </summary>
  string Provider { get; }

  /// <summary>
  /// Читает документы из источника данных.
  /// </summary>
  /// <param name="sourceName">Имя профиля источника данных.</param>
  /// <param name="options">Настройки источника данных.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Документы для построения поискового индекса.</returns>
  Task<IReadOnlyList<SearchDataSourceDocument>> ReadAsync(
      string sourceName,
      SearchDataSourceOptions options,
      CancellationToken cancellationToken = default);
}