using SearchEngine;

namespace SearchEngine.Service;

/// <summary>
/// Один поисковый индекс сервиса: его готовый снимок и шлюз построения.
/// </summary>
/// <remarks>
/// Каждый именованный индекс изолирован в собственном слоте, поэтому построение одного
/// индекса не мешает построению и поиску по другим. Чтение готового снимка выполняется
/// без локов через <see cref="Volatile"/>, а построение сериализуется lock-free шлюзом
/// <see cref="SingleFlightGate"/>.
/// </remarks>
internal sealed class SearchIndexSlot
{
  private readonly SingleFlightGate _buildGate = new();

  private SearchIndexSnapshot? _snapshot;

  /// <summary>
  /// Пытается начать построение индекса.
  /// </summary>
  /// <returns><see langword="true"/>, если построение можно начать;
  /// <see langword="false"/>, если построение этого индекса уже выполняется.</returns>
  public bool TryBeginBuild() => _buildGate.TryEnter();

  /// <summary>
  /// Завершает построение индекса.
  /// </summary>
  public void EndBuild() => _buildGate.Exit();

  /// <summary>
  /// Публикует готовый снимок индекса для поиска.
  /// </summary>
  /// <param name="search">Готовый поисковый индекс.</param>
  /// <param name="status">Состояние готового индекса.</param>
  public void Publish(Search<int> search, IndexStatusResponse status)
      => Volatile.Write(ref _snapshot, new SearchIndexSnapshot(search, status));

  /// <summary>
  /// Возвращает текущее состояние индекса.
  /// </summary>
  /// <returns>Состояние индекса.</returns>
  public IndexStatusResponse GetStatus()
  {
    SearchIndexSnapshot? snapshot = Volatile.Read(ref _snapshot);
    bool isBuilding = _buildGate.IsInProgress;

    if (snapshot is null)
      return new IndexStatusResponse
      {
        State = isBuilding ? IndexState.Building : IndexState.NotBuilt
      };

    return isBuilding
        ? snapshot.Status with { State = IndexState.Building }
        : snapshot.Status;
  }

  /// <summary>
  /// Выполняет простой поиск по текущему снимку индекса.
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
                    Ids = [.. item.Value.Items]
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
}
