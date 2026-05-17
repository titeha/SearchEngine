namespace SearchEngine.Service;

/// <summary>
/// Читает документы из in-memory источника данных, заданного в конфигурации сервиса.
/// </summary>
public sealed class InMemorySearchDataSourceReader : ISearchDataSourceReader
{
  /// <summary>
  /// Имя provider-а in-memory источника данных.
  /// </summary>
  public const string ProviderName = "in-memory";

  /// <inheritdoc />
  public string Provider => ProviderName;

  /// <inheritdoc />
  public Task<IReadOnlyList<SearchDataSourceDocument>> ReadAsync(
      string sourceName,
      SearchDataSourceOptions options,
      CancellationToken cancellationToken = default)
  {
    IReadOnlyList<SearchDataSourceDocument> documents =
    [
        .. options.Documents.Select(document => new SearchDataSourceDocument
            {
                Id = document.Id,
                Text = document.Text
            })
    ];

    return Task.FromResult(documents);
  }
}