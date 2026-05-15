namespace SearchEngine.Service;

/// <summary>
/// Документ, полученный из внешнего источника данных для построения поискового индекса.
/// </summary>
public sealed record SearchDataSourceDocument
{
  /// <summary>
  /// Получает идентификатор документа.
  /// </summary>
  public int Id { get; init; }

  /// <summary>
  /// Получает текст документа для индексации.
  /// </summary>
  public string Text { get; init; } = string.Empty;
}