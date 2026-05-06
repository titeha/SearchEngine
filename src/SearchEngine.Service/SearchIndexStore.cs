namespace SearchEngine.Service;

/// <summary>
/// Хранит текущее состояние поискового индекса сервиса.
/// </summary>
public sealed class SearchIndexStore
{
  private readonly object _lock = new();

  private IndexStatusResponse _status = new();

  /// <summary>
  /// Возвращает текущее состояние поискового индекса.
  /// </summary>
  /// <returns>Состояние поискового индекса.</returns>
  public IndexStatusResponse GetStatus()
  {
    lock (_lock)
    {
      return _status;
    }
  }
}