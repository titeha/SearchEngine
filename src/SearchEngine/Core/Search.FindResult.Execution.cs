using StringFunctions;

namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Выполняет поиск по подготовленному набору слов.
  /// </summary>
  /// <param name="searchItems">Слова поискового запроса.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <param name="matchMode">Режим объединения слов запроса.</param>
  /// <returns>Результат поиска.</returns>
  private SearchResultList<T> ExecuteSearch(
    IReadOnlyList<string> searchItems,
    SearchExecutionOptions executionOptions,
    QueryMatchMode matchMode)
  {
    if (searchItems.Count == 1
      && !IsPhoneticSearch
      && executionOptions.SearchType == SearchType.ExactSearch)
    {
      IReadOnlyList<T> indexes = ExecuteExactZeroDistanceItemSearch(
        searchItems[0],
        executionOptions.SearchLocation);

      return BuildExactZeroDistanceSingleTermResult(indexes);
    }

    if (searchItems.Count == 1)
      return ExecuteSingleTermSearch(searchItems[0], executionOptions);

    if (!IsPhoneticSearch && executionOptions.SearchType == SearchType.ExactSearch)
    {
      IReadOnlyList<T>[] termIndexes = ExecuteExactZeroDistanceTermSearches(
        searchItems,
        executionOptions);

      return BuildExactZeroDistanceSearchResult(
        termIndexes,
        matchMode);
    }

    return matchMode switch
    {
      QueryMatchMode.AllTerms => ExecuteAllTermsSearch(searchItems, executionOptions),
      QueryMatchMode.AnyTerm => ExecuteAnyTermSearch(searchItems, executionOptions),
      QueryMatchMode.SoftAllTerms => ExecuteSoftAllTermsSearch(searchItems, executionOptions),

      _ => throw new InvalidOperationException(
        $"Режим {matchMode} не поддерживается текущей реализацией поиска.")
    };
  }

  /// <summary>
  /// Выполняет поиск по одному слову.
  /// </summary>
  /// <param name="item">Искомое слово.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <returns>Результат поиска по одному слову.</returns>
  private SearchResultList<T> ExecuteSingleItemSearch(string item, SearchExecutionOptions executionOptions)
  {
    if (IsPhoneticSearch)
      return PhoneticFind(item);

    if (item.Length == 2 || executionOptions.SearchType == SearchType.ExactSearch)
      return ExactSearch(item, executionOptions.SearchLocation);

    int distance = executionOptions.AcceptableCountMisprint >= 0
      ? executionOptions.AcceptableCountMisprint
      : CalculateDistance(item.Length, executionOptions.PrecisionSearch);

    return FuzzySearch(item, distance, executionOptions.SearchLocation);
  }

  /// <summary>
  /// Выполняет поиск по одному слову запроса без маршрутизации через режимы объединения.
  /// </summary>
  /// <param name="item">Искомое слово.</param>
  /// <param name="executionOptions">Эффективные параметры выполнения поиска.</param>
  /// <returns>Нормализованный результат поиска по одному слову.</returns>
  private SearchResultList<T> ExecuteSingleTermSearch(
    string item,
    SearchExecutionOptions executionOptions)
  {
    SearchResultList<T> searchResult = ExecuteSingleItemSearch(item, executionOptions);

    return BuildSingleTermSearchResult(searchResult);
  }

  /// <summary>
  /// Формирует эффективные параметры выполнения поиска
  /// с учётом настроек экземпляра и параметров запроса.
  /// </summary>
  /// <param name="request">Параметры поиска.</param>
  /// <returns>Параметры выполнения конкретного запроса.</returns>
  private SearchExecutionOptions CreateExecutionOptions(SearchRequest? request) => new(
    request?.SearchType ?? SearchType,
    request?.SearchLocation ?? SearchLocation,
    request?.PrecisionSearch ?? PrecisionSearch,
    request?.AcceptableCountMisprint ?? AcceptableCountMisprint);

  /// <summary>
  /// Разбирает поисковую строку в локальный набор слов,
  /// не изменяя состояние экземпляра.
  /// </summary>
  /// <param name="source">Исходная поисковая строка.</param>
  /// <returns>Упорядоченный набор пригодных для поиска слов.</returns>
  private string[] DisassembleSearchTerms(string source)
  {
    string clearedString = source.Trim().ToUpper();

    if (clearedString.IsNullOrWhiteSpace())
      return [];

    SortedSet<string> searchItems = new();
    var delimiterArray = IndexBuilder.Delimiters.ToCharArray();
    var values = clearedString.Split(delimiterArray, StringSplitOptions.RemoveEmptyEntries);

    for (int i = 0, count = values.Length; i < count; i++)
      if (values[i].Length > 1)
        searchItems.Add(IsPhoneticSearch ? PhoneticSearch.MetaPhone(values[i]) : values[i]);

    return [.. searchItems];
  }

  /// <summary>
  /// Определяет, относится ли исключение к критическим,
  /// которые не нужно скрывать за результатом.
  /// </summary>
  /// <param name="exception">Проверяемое исключение.</param>
  /// <returns>
  /// <see langword="true"/>, если исключение критическое;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool IsCriticalException(Exception exception) =>
    exception is OutOfMemoryException
    or StackOverflowException
    or AccessViolationException
    or AppDomainUnloadedException
    or BadImageFormatException
    or CannotUnloadAppDomainException;
}
