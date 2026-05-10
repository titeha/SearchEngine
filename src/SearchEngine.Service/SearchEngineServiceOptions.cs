namespace SearchEngine.Service;

/// <summary>
/// Настройки поискового сервиса.
/// </summary>
public sealed class SearchEngineServiceOptions
{
  /// <summary>
  /// Получает или задаёт максимальное количество документов для построения индекса.
  /// </summary>
  public int MaxDocumentCount { get; set; } = 100_000;

  /// <summary>
  /// Получает или задаёт максимальную длину текста одного документа.
  /// </summary>
  public int MaxDocumentTextLength { get; set; } = 10_000;
}