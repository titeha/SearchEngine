using System.Collections.Concurrent;

using Microsoft.Extensions.Options;

using SearchEngine;

namespace SearchEngine.Service;

/// <summary>
/// Хранит поисковые индексы сервиса.
/// </summary>
/// <remarks>
/// Сервис поддерживает несколько именованных индексов. Каждый индекс изолирован в собственном
/// слоте <see cref="SearchIndexSlot"/>, поэтому индексы строятся параллельно и не мешают друг
/// другу. Имя индекса необязательно: если оно не задано, используется индекс по умолчанию.
/// </remarks>
/// <param name="options">Настройки поискового сервиса.</param>
/// <param name="snapshotStorage">Файловое хранилище snapshot индексов.</param>
public sealed class SearchIndexStore(IOptions<SearchEngineServiceOptions> options, SearchIndexSnapshotStorage snapshotStorage)
{
  /// <summary>
  /// Имя индекса по умолчанию.
  /// </summary>
  public const string DefaultIndexName = "default";

  private const int _maxIndexNameLength = 64;

  private readonly SearchEngineServiceOptions _options = options.Value;
  private readonly SearchIndexSnapshotStorage _snapshotStorage = snapshotStorage;

  private readonly ConcurrentDictionary<string, SearchIndexSlot> _slots =
      new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Создаёт хранилище поисковых индексов с настройками по умолчанию.
  /// </summary>
  public SearchIndexStore()
      : this(Options.Create(new SearchEngineServiceOptions())) { }

  /// <summary>
  /// Создаёт хранилище поисковых индексов.
  /// </summary>
  /// <param name="options">Настройки поискового сервиса.</param>
  public SearchIndexStore(IOptions<SearchEngineServiceOptions> options)
      : this(options, new SearchIndexSnapshotStorage(options)) { }

  /// <summary>
  /// Возвращает текущее состояние индекса.
  /// </summary>
  /// <param name="indexName">Имя индекса. Если не задано, используется индекс по умолчанию.</param>
  /// <returns>Состояние индекса.</returns>
  public IndexStatusResponse GetStatus(string? indexName = null)
  {
    if (!TryResolveIndexName(indexName, out string name, out _))
      return new IndexStatusResponse { IndexName = indexName?.Trim() ?? string.Empty };

    if (!_slots.TryGetValue(name, out SearchIndexSlot? slot))
      return new IndexStatusResponse { IndexName = name };

    return slot.GetStatus() with { IndexName = name };
  }

  /// <summary>
  /// Возвращает состояние всех известных индексов.
  /// </summary>
  /// <returns>Состояния индексов, отсортированные по имени.</returns>
  public IReadOnlyList<IndexStatusResponse> GetAllStatuses()
  {
    return
    [
        .. _slots
            .OrderBy(slot => slot.Key, StringComparer.OrdinalIgnoreCase)
            .Select(slot => slot.Value.GetStatus() with { IndexName = slot.Key })
    ];
  }

  /// <summary>
  /// Полностью перестраивает поисковый индекс.
  /// </summary>
  /// <param name="request">Запрос на построение индекса.</param>
  /// <returns>Ошибка построения индекса или <see langword="null"/>, если индекс построен успешно.</returns>
  public async Task<ApiError?> BuildAsync(IndexBuildRequest request)
  {
    if (!TryResolveIndexName(request.Index, out string indexName, out ApiError? indexNameError))
      return indexNameError;

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

    SearchIndexSlot slot = GetOrAddSlot(indexName);

    if (!slot.TryBeginBuild())
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
            .SaveAsync(snapshotFile, indexName)
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
        IndexName = indexName,
        State = IndexState.Ready,
        IsReady = true,
        DocumentCount = request.Documents.Count,
        SearchableDocumentCount = documents.Length,
        IsPhoneticSearch = request.IsPhoneticSearch,
        CreatedAtUtc = createdAtUtc
      };

      slot.Publish(search, status);

      return null;
    }
    finally
    {
      slot.EndBuild();
    }
  }

  /// <summary>
  /// Восстанавливает поисковый индекс из snapshot-файла.
  /// </summary>
  /// <param name="indexName">Имя индекса. Если не задано, используется индекс по умолчанию.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Ошибка восстановления или <see langword="null"/>, если индекс восстановлен успешно.</returns>
  public async Task<ApiError?> RestoreAsync(string? indexName = null, CancellationToken cancellationToken = default)
  {
    if (!TryResolveIndexName(indexName, out string name, out ApiError? indexNameError))
      return indexNameError;

    if (!_options.Snapshot.IsEnabled)
      return new ApiError
      {
        Code = "SnapshotDisabled",
        Message = "Восстановление snapshot поискового индекса отключено."
      };

    SearchIndexSlot slot = GetOrAddSlot(name);

    if (!slot.TryBeginBuild())
      return null;

    try
    {
      SearchIndexSnapshotFile? snapshotFile;

      try
      {
        snapshotFile = await _snapshotStorage
            .LoadAsync(name, cancellationToken)
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
        IndexName = name,
        State = IndexState.Ready,
        IsReady = true,
        DocumentCount = snapshotFile.Documents.Count,
        SearchableDocumentCount = documents.Length,
        IsPhoneticSearch = snapshotFile.IsPhoneticSearch,
        CreatedAtUtc = snapshotFile.CreatedAtUtc
      };

      slot.Publish(search, status);

      return null;
    }
    finally
    {
      slot.EndBuild();
    }
  }

  /// <summary>
  /// Выполняет простой поиск по индексу.
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

    if (!TryResolveIndexName(request.Index, out string name, out ApiError? indexNameError))
    {
      error = indexNameError;
      return null;
    }

    if (!_slots.TryGetValue(name, out SearchIndexSlot? slot))
    {
      error = new ApiError
      {
        Code = "IndexNotBuilt",
        Message = "Поисковый индекс ещё не построен."
      };

      return null;
    }

    return slot.Search(request, out error);
  }

  /// <summary>
  /// Возвращает слот индекса по имени, создавая его при необходимости.
  /// </summary>
  /// <param name="indexName">Нормализованное имя индекса.</param>
  /// <returns>Слот индекса.</returns>
  private SearchIndexSlot GetOrAddSlot(string indexName)
      => _slots.GetOrAdd(indexName, _ => new SearchIndexSlot());

  /// <summary>
  /// Нормализует и проверяет имя индекса.
  /// </summary>
  /// <param name="indexName">Имя индекса из запроса или <see langword="null"/>.</param>
  /// <param name="normalizedName">Нормализованное имя индекса.</param>
  /// <param name="error">Ошибка валидации имени индекса.</param>
  /// <returns><see langword="true"/>, если имя индекса корректно.</returns>
  private static bool TryResolveIndexName(string? indexName, out string normalizedName, out ApiError? error)
  {
    if (string.IsNullOrWhiteSpace(indexName))
    {
      normalizedName = DefaultIndexName;
      error = null;
      return true;
    }

    string trimmed = indexName.Trim();

    if (!IsValidIndexName(trimmed))
    {
      normalizedName = string.Empty;
      error = new ApiError
      {
        Code = "InvalidIndexName",
        Message = $"Недопустимое имя индекса: {indexName}. Разрешены латинские буквы, цифры, '-' и '_' (до {_maxIndexNameLength} символов)."
      };

      return false;
    }

    normalizedName = trimmed;
    error = null;
    return true;
  }

  /// <summary>
  /// Проверяет, что имя индекса состоит только из безопасных символов.
  /// </summary>
  /// <remarks>
  /// Имя индекса используется для формирования имени snapshot-файла, поэтому набор символов
  /// ограничен, чтобы исключить выход за пределы каталога snapshot.
  /// </remarks>
  /// <param name="indexName">Имя индекса.</param>
  /// <returns><see langword="true"/>, если имя индекса безопасно.</returns>
  private static bool IsValidIndexName(string indexName)
  {
    if (indexName.Length > _maxIndexNameLength)
      return false;

    foreach (char character in indexName)
      if (!char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_')
        return false;

    return true;
  }

  /// <summary>
  /// Документ, передаваемый в библиотеку SearchEngine для индексации.
  /// </summary>
  /// <param name="Id">Идентификатор документа.</param>
  /// <param name="Text">Текст документа.</param>
  private sealed record IndexDocument(int Id, string Text) : ISourceData<int>;
}
