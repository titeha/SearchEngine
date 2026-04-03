namespace SearchEngine;

/// <summary>
/// Параметры выполнения конкретного поискового запроса.
/// </summary>
public sealed class SearchRequest
{
  /// <summary>
  /// Режим объединения слов поискового запроса.
  /// </summary>
  public QueryMatchMode MatchMode { get; init; } = QueryMatchMode.AllTerms;

  /// <summary>
  /// Тип поискового механизма.
  /// Если не задан, используется значение из экземпляра <see cref="Search{T}"/>.
  /// </summary>
  public SearchType? SearchType { get; init; }

  /// <summary>
  /// Место поиска.
  /// Если не задано, используется значение из экземпляра <see cref="Search{T}"/>.
  /// </summary>
  public SearchLocation? SearchLocation { get; init; }

  /// <summary>
  /// Точность поиска в процентах.
  /// Если не задана, используется значение из экземпляра <see cref="Search{T}"/>.
  /// </summary>
  public int? PrecisionSearch { get; init; }

  /// <summary>
  /// Допустимое количество опечаток.
  /// Если не задано, используется значение из экземпляра <see cref="Search{T}"/>.
  /// </summary>
  public int? AcceptableCountMisprint { get; init; }
}