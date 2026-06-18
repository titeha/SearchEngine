using Microsoft.Extensions.Options;

using SearchEngine;

namespace SearchEngine.Service;

/// <summary>
/// Хранит текущий поисковый индекс сервиса.
/// </summary>
/// <remarks>
/// Создаёт хранилище поискового индекса.
/// </remarks>
/// <param name="options">Настройки поискового сервиса.</param>
public sealed class SearchIndexStore(IOptions<SearchEngineServiceOptions> options, SearchIndexSnapshotStorage snapshotStorage)
{
  private readonly SearchEngineServiceOptions _options = options.Value;
  private readonly SearchIndexSnapshotStorage _snapshotStorage = snapshotStorage;

  private readonly SearchIndexSlot _defaultSlot = new();

  /// <summary>
  /// Создаёт хранилище поискового индекса с настройками по умолчанию.
  /// </summary>
  public SearchIndexStore()
      : this(Options.Create(new SearchEngineServiceOptions())) { }

  /// <summary>
  /// Создаёт хранилище поискового индекса.
  /// </summary>
  /// <param name="options">Настройки поискового сервиса.</param>
  public SearchIndexStore(IOptions<SearchEngineServiceOptions> options)
      : this(options, new SearchIndexSnapshotStorage(options)) { }

  /// <summary>
  /// Возвращает текущее состояние поискового индекса.
  /// </summary>
  /// <returns>Состояние поискового индекса.</returns>
  public IndexStatusResponse GetStatus() => _defaultSlot.GetStatus();

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

    if (!_defaultSlot.TryBeginBuild())
      return null;

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

      SearchIndexSnapshotFile snapshotFile = new()
      {
        Version = 1,
        IsPhoneticSearch = request.IsPhoneticSearch,
        CreatedAtUtc = createdAtUtc,
        Documents = [.. documents.Select(document => new SearchIndexSnapshotDocument
        {
          Id = document.Id,
          Text = document.Text
        })]
      };

      try
      {
        await _snapshotStorage
            .SaveAsync(snapshotFile)
            .ConfigureAwait(false);
      }
      catch (Exception exception)
      {
        return new ApiError
        {
          Code = "SnapshotSaveFailed",
          Message = $"Не удалось сохранить snapshot поискового индекса: {exception.Message}"
        };
      }

      IndexStatusResponse status = new()
      {
        State = IndexState.Ready,
        IsReady = true,
        DocumentCount = request.Documents.Count,
        SearchableDocumentCount = documents.Length,
        IsPhoneticSearch = request.IsPhoneticSearch,
        CreatedAtUtc = createdAtUtc
      };

      _defaultSlot.Publish(search, status);

      return null;
    }
    finally
    {
      _defaultSlot.EndBuild();
    }
  }

  /// <summary>
  /// Восстанавливает поисковый индекс из snapshot-файла.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Ошибка восстановления или <see langword="null"/>, если индекс восстановлен успешно.</returns>
  public async Task<ApiError?> RestoreAsync(CancellationToken cancellationToken = default)
  {
    if (!_options.Snapshot.IsEnabled)
      return new ApiError
      {
        Code = "SnapshotDisabled",
        Message = "Восстановление snapshot поискового индекса отключено."
      };

    if (!_defaultSlot.TryBeginBuild())
      return null;

    try
    {
      SearchIndexSnapshotFile? snapshotFile;

      try
      {
        snapshotFile = await _snapshotStorage
            .LoadAsync(cancellationToken)
            .ConfigureAwait(false);
      }
      catch (Exception exception)
      {
        return new ApiError
        {
          Code = "SnapshotLoadFailed",
          Message = $"Не удалось загрузить snapshot поискового индекса: {exception.Message}"
        };
      }

      if (snapshotFile is null)
        return new ApiError
        {
          Code = "SnapshotNotFound",
          Message = "Snapshot-файл поискового индекса не найден."
        };

      if (snapshotFile.Version != 1)
        return new ApiError
        {
          Code = "UnsupportedSnapshotVersion",
          Message = $"Версия snapshot-файла не поддерживается: {snapshotFile.Version}."
        };

      if (snapshotFile.Documents is null || snapshotFile.Documents.Count == 0)
        return new ApiError
        {
          Code = "SnapshotHasNoDocuments",
          Message = "Snapshot-файл не содержит документов для восстановления индекса."
        };

      if (snapshotFile.Documents.Count > _options.MaxDocumentCount)
        return new ApiError
        {
          Code = "TooManyDocuments",
          Message = $"Количество документов превышает допустимое значение: {_options.MaxDocumentCount}."
        };

      SearchIndexSnapshotDocument? tooLongDocument = snapshotFile.Documents
          .FirstOrDefault(document => document.Text.Length > _options.MaxDocumentTextLength);

      if (tooLongDocument is not null)
        return new ApiError
        {
          Code = "DocumentTextTooLong",
          Message = $"Длина текста документа превышает допустимое значение: {_options.MaxDocumentTextLength}."
        };

      IndexDocument[] documents =
      [
          .. snapshotFile.Documents
                .Where(document => !string.IsNullOrWhiteSpace(document.Text))
                .Select(document => new IndexDocument(document.Id, document.Text))
      ];

      if (documents.Length == 0)
      {
        return new ApiError
        {
          Code = "SnapshotHasNoSearchableDocuments",
          Message = "Snapshot-файл не содержит документов с пригодным для индексации текстом."
        };
      }

      Search<int> search = new(snapshotFile.IsPhoneticSearch);

      var prepareResult = await search
          .PrepareIndexResult(documents)
          .ConfigureAwait(false);

      if (prepareResult.IsFailure)
        return new ApiError
        {
          Code = prepareResult.Error!.Code.ToString(),
          Message = prepareResult.Error.Message
        };

      IndexStatusResponse status = new()
      {
        State = IndexState.Ready,
        IsReady = true,
        DocumentCount = snapshotFile.Documents.Count,
        SearchableDocumentCount = documents.Length,
        IsPhoneticSearch = snapshotFile.IsPhoneticSearch,
        CreatedAtUtc = snapshotFile.CreatedAtUtc
      };

      _defaultSlot.Publish(search, status);

      return null;
    }
    finally
    {
      _defaultSlot.EndBuild();
    }
  }

  /// <summary>
  /// Выполняет простой поиск по текущему индексу.
  /// </summary>
  /// <param name="request">Запрос на выполнение поиска.</param>
  /// <param name="error">Ошибка поиска, если операция завершилась неуспешно.</param>
  /// <returns>Ответ поиска или <see langword="null"/>, если поиск выполнить не удалось.</returns>
  public SearchQueryResponse? Search(SearchQueryRequest request, out ApiError? error)
      => _defaultSlot.Search(request, out error);

  /// <summary>
  /// Документ, передаваемый в библиотеку SearchEngine для индексации.
  /// </summary>
  /// <param name="Id">Идентификатор документа.</param>
  /// <param name="Text">Текст документа.</param>
  private sealed record IndexDocument(int Id, string Text) : ISourceData<int>;
}
