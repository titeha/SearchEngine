using System.Text.Json.Serialization;

namespace SearchEngine.Service;

/// <summary>
/// Состояние поискового индекса.
/// </summary>
/// <remarks>
/// Сериализуется строкой независимо от настроек сериализатора, чтобы значение
/// одинаково читалось и сервисом, и его клиентами.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<IndexState>))]
public enum IndexState
{
  /// <summary>
  /// Индекс ещё не построен.
  /// </summary>
  NotBuilt,

  /// <summary>
  /// Индекс сейчас строится.
  /// </summary>
  Building,

  /// <summary>
  /// Индекс построен и готов к поиску.
  /// </summary>
  Ready
}
