using Microsoft.Extensions.Options;

namespace SearchEngine.Service;

/// <summary>
/// Строит поисковый индекс из заранее настроенного источника данных.
/// </summary>
public sealed class SearchIndexFromSourceBuilder
{
  private readonly SearchEngineServiceOptions _options;
  private readonly SearchDataSourceReaderRegistry _registry;
  private readonly SearchDataSourceProfileValidator _validator;
  private readonly SearchIndexStore _store;

  /// <summary>
  /// Создаёт сервис построения индекса из источника данных.
  /// </summary>
  /// <param name="options">Настройки поискового сервиса.</param>
  /// <param name="registry">Registry provider-ов источников данных.</param>
  /// <param name="validator">Валидатор профилей источников данных.</param>
  /// <param name="store">Хранилище поискового индекса.</param>
  public SearchIndexFromSourceBuilder(
      IOptions<SearchEngineServiceOptions> options,
      SearchDataSourceReaderRegistry registry,
      SearchDataSourceProfileValidator validator,
      SearchIndexStore store)
  {
    _options = options.Value;
    _registry = registry;
    _validator = validator;
    _store = store;
  }

  /// <summary>
  /// Строит поисковый индекс из заранее настроенного источника данных.
  /// </summary>
  /// <param name="request">Запрос на построение индекса из источника данных.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Состояние индекса или ошибка построения.</returns>
  public async Task<(IndexStatusResponse? Status, ApiError? Error)> BuildAsync(
      IndexBuildFromSourceRequest request,
      CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(request.SourceName))
      return (null, new ApiError
      {
        Code = "EmptySourceName",
        Message = "Не указано имя источника данных."
      });

    string sourceName = request.SourceName.Trim();

    SearchDataSourceOptions? source = FindDataSource(sourceName);

    if (source is null)
      return (null, new ApiError
      {
        Code = "DataSourceNotFound",
        Message = $"Источник данных не найден: {sourceName}."
      });

    if (!source.IsEnabled)
      return (null, new ApiError
      {
        Code = "DataSourceDisabled",
        Message = $"Источник данных отключён: {sourceName}."
      });

    ApiError? validationError = _validator.Validate(sourceName, source);

    if (validationError is not null)
      return (null, validationError);

    ISearchDataSourceReader? reader = _registry.GetReader(source.Provider);

    if (reader is null)
      return (null, new ApiError
      {
        Code = "DataSourceProviderNotSupported",
        Message = $"Provider источника данных не поддерживается: {source.Provider}."
      });

    IReadOnlyList<SearchDataSourceDocument> sourceDocuments;

    try
    {
      sourceDocuments = await reader
          .ReadAsync(sourceName, source, cancellationToken)
          .ConfigureAwait(false);
    }
    catch (Exception exception)
    {
      return (null, new ApiError
      {
        Code = "DataSourceReadFailed",
        Message = $"Не удалось прочитать данные из источника: {exception.Message}"
      });
    }

    IndexBuildRequest buildRequest = new()
    {
      IsPhoneticSearch = request.IsPhoneticSearch,
      Documents =
        [
            .. sourceDocuments.Select(document => new IndexDocumentRequest
                {
                    Id = document.Id,
                    Text = document.Text
                })
        ]
    };

    ApiError? error = await _store
        .BuildAsync(buildRequest)
        .ConfigureAwait(false);

    if (error is not null)
      return (null, error);

    return (_store.GetStatus(), null);
  }

  /// <summary>
  /// Находит настройки источника данных по имени без учёта регистра.
  /// </summary>
  /// <param name="sourceName">Имя источника данных.</param>
  /// <returns>Настройки источника данных или <see langword="null"/>, если источник не найден.</returns>
  private SearchDataSourceOptions? FindDataSource(string sourceName)
  {
    foreach (KeyValuePair<string, SearchDataSourceOptions> source in _options.Sources)
      if (string.Equals(source.Key, sourceName, StringComparison.OrdinalIgnoreCase))
        return source.Value;

    return null;
  }
}