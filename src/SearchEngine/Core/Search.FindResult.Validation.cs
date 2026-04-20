namespace SearchEngine;

/// <summary>
/// Безопасные методы поиска, возвращающие результат выполнения.
/// </summary>
public partial class Search<T> where T : struct
{
  /// <summary>
  /// Проверяет корректность параметров поискового запроса.
  /// </summary>
  /// <param name="request">Параметры поиска.</param>
  /// <param name="error">Описание ошибки, если проверка не пройдена.</param>
  /// <returns>
  /// <see langword="true"/>, если параметры корректны;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool TryValidateRequest(SearchRequest? request, out SearchError? error)
  {
    if (request is null)
    {
      error = null;
      return true;
    }

    if (!Enum.IsDefined(request.MatchMode))
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Режим объединения слов запроса имеет недопустимое значение.");
      return false;
    }

    if (request.SearchType.HasValue &&
        !Enum.IsDefined(request.SearchType.Value))
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Тип поискового механизма имеет недопустимое значение.");
      return false;
    }

    if (request.SearchLocation.HasValue &&
        !Enum.IsDefined(request.SearchLocation.Value))
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Место поиска имеет недопустимое значение.");
      return false;
    }

    if (request.PrecisionSearch is < 0 or > 100)
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Точность поиска должна быть в диапазоне от 0 до 100.");
      return false;
    }

    if (request.AcceptableCountMisprint < 0)
    {
      error = new SearchError(
        SearchErrorCode.InvalidSearchRequest,
        "Допустимое количество опечаток не может быть отрицательным.");
      return false;
    }

    error = null;
    return true;
  }
}
