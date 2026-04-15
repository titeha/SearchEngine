namespace SearchEngine;

/// <summary>
/// Параметры выполнения конкретного поискового запроса.
/// </summary>
public sealed class SearchRequest
{
  /// <summary>
  /// Определяет, как объединяются слова поискового запроса при формировании результата.
  /// </summary>
  public QueryMatchMode MatchMode { get; init; } = QueryMatchMode.AllTerms;

  /// <summary>
  /// Переопределяет тип поиска для текущего запроса.
  /// Если значение не задано, используется настройка экземпляра <see cref="Search{T}"/>.
  /// </summary>
  public SearchType? SearchType { get; init; }

  /// <summary>
  /// Переопределяет место поиска для текущего запроса.
  /// Если значение не задано, используется настройка экземпляра <see cref="Search{T}"/>.
  /// </summary>
  public SearchLocation? SearchLocation { get; init; }

  /// <summary>
  /// Переопределяет точность неточного поиска в процентах для текущего запроса.
  /// Если значение не задано, используется настройка экземпляра <see cref="Search{T}"/>.
  /// </summary>
  public int? PrecisionSearch { get; init; }

  /// <summary>
  /// Переопределяет допустимое количество опечаток для текущего запроса.
  /// Если значение не задано, используется настройка экземпляра <see cref="Search{T}"/>.
  /// </summary>
  public int? AcceptableCountMisprint { get; init; }
}
