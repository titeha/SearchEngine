using ResultType;

namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
/// <typeparam name="T">Тип идентификатора записи.</typeparam>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Выполняет поиск и возвращает результат без выбрасывания исключений
  /// в ожидаемых прикладных сценариях.
  /// </summary>
  /// <param name="searchString">Поисковый запрос.</param>
  /// <returns>
  /// Успешный результат поиска либо описание ошибки.
  /// </returns>
  public Result<SearchResultList<T>, SearchError> FindResult(string searchString)
  {
    if (string.IsNullOrWhiteSpace(searchString))
      return Result.Failure<SearchResultList<T>, SearchError>(
                new SearchError(
                    SearchErrorCode.EmptyQuery,
                    "Поисковый запрос пуст."));

    if (!IsIndexComplete)
      return Result.Failure<SearchResultList<T>, SearchError>(
                new SearchError(
                    SearchErrorCode.IndexNotBuilt,
                    "Поисковый индекс не подготовлен."));

    if (_searchIndex is null || _searchIndex.Count == 0)
      return Result.Failure<SearchResultList<T>, SearchError>(
                new SearchError(
                    SearchErrorCode.IndexIsEmpty,
                    "Поисковый индекс пуст."));

    try
    {
      DisassemblyString(searchString);

      if (_searchList.Count == 0)
      {
        return Result.Failure<SearchResultList<T>, SearchError>(
            new SearchError(
                SearchErrorCode.QueryHasNoSearchableTerms,
                "Поисковый запрос не содержит пригодных для поиска слов."));
      }

      SearchResultList<T> searchResult = new();

      foreach (string item in _searchList)
        if (IsPhoneticSearch)
          searchResult.Union(PhoneticFind(item));
        else if (item.Length == 2 || SearchType.ExactSearch == SearchType)
          searchResult.Union(ExactSearch(item));
        else
          searchResult.Union(FusySearch(item, CalculateDistance(item.Length, PrecisionSearch)));

      return Result.Success<SearchResultList<T>, SearchError>(searchResult);
    }
    catch (Exception exception) when (!IsCriticalException(exception))
    {
      return Result.Failure<SearchResultList<T>, SearchError>(
          new SearchError(
              SearchErrorCode.SearchExecutionFailed,
              "Во время выполнения поиска произошла ошибка.",
              exception));
    }
  }

  /// <summary>
  /// Вычисляет допустимую дистанцию поиска по длине слова и точности.
  /// </summary>
  /// <param name="length">Длина искомого слова.</param>
  /// <param name="percent">Точность поиска в процентах.</param>
  /// <returns>Допустимая дистанция.</returns>
  private static int CalculateDistance(int length, int percent) =>
      length > 1
        ? length - length * percent / 100
        : 0;

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