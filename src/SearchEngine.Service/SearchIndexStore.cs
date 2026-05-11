using Microsoft.Extensions.Options;

namespace SearchEngine.Service;

/// <summary>
/// Хранит текущий поисковый индекс сервиса.
/// </summary>
/// <remarks>
/// Создаёт хранилище поискового индекса.
/// </remarks>
/// <param name="options">Настройки поискового сервиса.</param>
public sealed class SearchIndexStore(IOptions<SearchEngineServiceOptions> options)
{
  private readonly SemaphoreSlim _buildLock = new(1, 1);
  private readonly SearchEngineServiceOptions _options = options.Value;

  private SearchIndexSnapshot? _snapshot;

  /// <summary>
  /// Создаёт хранилище поискового индекса с настройками по умолчанию.
  /// </summary>
  public SearchIndexStore()
      : this(Options.Create(new SearchEngineServiceOptions())) { }

  /// <summary>
  /// Возвращает текущее состояние поискового индекса.
  /// </summary>
  /// <returns>Состояние поискового индекса.</returns>
  public IndexStatusResponse GetStatus()
  {
    SearchIndexSnapshot? snapshot = Volatile.Read(ref _snapshot);

    if (snapshot is null)
      return new IndexStatusResponse();

    return snapshot.Status;
  }

  /// <summary>
  /// Полностью перестраивает поисковый индекс.
  /// </summary>
  /// <param name="request">Запрос на построение индекса.</param>
  /// <returns>Ошибка построения индекса или <see langword="null"/>, если индекс построен успешно.</returns>
  public async Task<ApiError?> BuildAsync(IndexBuildRequest request)
  {
    if (request.Documents is null || request.Documents.Count == 0)
      return new ApiError
      {
        Code = "EmptyDocuments",
        Message = "Не переданы документы для индексации."
      };

    if (request.Documents.Count > _options.MaxDocumentCount)
      return new ApiError
      {
        Code = "TooManyDocuments",
        Message = $"Количество документов превышает допустимое значение: {_options.MaxDocumentCount}."
      };

    IndexDocumentRequest? tooLongDocument = request.Documents
        .FirstOrDefault(document => document?.Text?.Length > _options.MaxDocumentTextLength);

    if (tooLongDocument is not null)
      return new ApiError
      {
        Code = "DocumentTextTooLong",
        Message = $"Длина текста документа превышает допустимое значение: {_options.MaxDocumentTextLength}."
      };

    IndexDocument[] documents =
    [
        .. request.Documents
                .Where(document => !string.IsNullOrWhiteSpace(document?.Text))
                .Select(document => new IndexDocument(document!.Id, document.Text!))
    ];

    if (documents.Length == 0)
      return new ApiError
      {
        Code = "EmptySearchableDocuments",
        Message = "Документы не содержат пригодного для индексации текста."
      };

    await _buildLock.WaitAsync().ConfigureAwait(false);

    try
    {
      Search<int> search = new(request.IsPhoneticSearch);

      var prepareResult = await search
          .PrepareIndexResult(documents)
          .ConfigureAwait(false);

      if (prepareResult.IsFailure)
        return new ApiError
        {
          Code = prepareResult.Error!.Code.ToString(),
          Message = prepareResult.Error.Message
        };

      DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;

      IndexStatusResponse status = new()
      {
        IsReady = true,
        DocumentCount = request.Documents.Count,
        SearchableDocumentCount = documents.Length,
        IsPhoneticSearch = request.IsPhoneticSearch,
        CreatedAtUtc = createdAtUtc
      };

      SearchIndexSnapshot snapshot = new(search, status);

      Volatile.Write(ref _snapshot, snapshot);

      return null;
    }
    finally
    {
      _buildLock.Release();
    }
  }

  /// <summary>
  /// Выполняет простой поиск по текущему индексу.
  /// </summary>
  /// <param name="request">Запрос на выполнение поиска.</param>
  /// <param name="error">Ошибка поиска, если операция завершилась неуспешно.</param>
  /// <returns>Ответ поиска или <see langword="null"/>, если поиск выполнить не удалось.</returns>
  public SearchQueryResponse? Search(SearchQueryRequest request, out ApiError? error)
  {
    if (string.IsNullOrWhiteSpace(request.Query))
    {
      error = new ApiError
      {
        Code = "EmptyQuery",
        Message = "Поисковая строка пуста."
      };

      return null;
    }

    SearchIndexSnapshot? snapshot = Volatile.Read(ref _snapshot);

    if (snapshot is null)
    {
      error = new ApiError
      {
        Code = "IndexNotBuilt",
        Message = "Поисковый индекс ещё не построен."
      };

      return null;
    }

    SearchRequest searchRequest = new()
    {
      MatchMode = request.MatchMode,
      SearchType = request.SearchType,
      SearchLocation = request.SearchLocation,
      PrecisionSearch = request.PrecisionSearch,
      AcceptableCountMisprint = request.AcceptableCountMisprint
    };

    var searchResult = snapshot.Search.FindResult(request.Query, searchRequest);

    if (searchResult.IsFailure)
    {
      error = new ApiError
      {
        Code = searchResult.Error!.Code.ToString(),
        Message = searchResult.Error.Message
      };

      return null;
    }

    SearchResultBucket[] items =
    [
        .. searchResult.Value!.Items
                .Select(item => new SearchResultBucket
                {
                    Key = item.Key,
                    Ids = item.Value.Items.ToArray()
                })
    ];

    error = null;

    return new SearchQueryResponse
    {
      IsHasIndex = searchResult.Value.IsHasIndex,
      Items = items
    };
  }

  /// <summary>
  /// Готовый снимок поискового индекса.
  /// </summary>
  /// <param name="Search">Готовый поисковый индекс.</param>
  /// <param name="Status">Состояние готового поискового индекса.</param>
  private sealed record SearchIndexSnapshot(
      Search<int> Search,
      IndexStatusResponse Status);

  /// <summary>
  /// Документ, передаваемый в библиотеку SearchEngine для индексации.
  /// </summary>
  /// <param name="Id">Идентификатор документа.</param>
  /// <param name="Text">Текст документа.</param>
  private sealed record IndexDocument(int Id, string Text) : ISourceData<int>;
}