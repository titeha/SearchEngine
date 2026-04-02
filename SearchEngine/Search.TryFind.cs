using ResultType;

namespace SearchEngine;

public partial class Search<T> where T : struct
{
    /// <summary>
    /// Безопасная версия поиска, не выбрасывающая исключения в ожидаемых сценариях.
    /// </summary>
    /// <param name="searchString">Поисковая строка.</param>
    /// <returns>
    /// Успешный результат со списком найденных записей или ошибка поиска.
    /// </returns>
    public Result<SearchResultList<T>, SearchEngineError> TryFind(string? searchString)
    {
        if (searchString is null)
            return Result.Failure<SearchResultList<T>, SearchEngineError>(SearchEngineError.NullSearchString());

        if (string.IsNullOrWhiteSpace(searchString))
            return Result.Success<SearchResultList<T>, SearchEngineError>(new SearchResultList<T>());

        if (!IsIndexComplete)
            return Result.Failure<SearchResultList<T>, SearchEngineError>(SearchEngineError.IndexNotBuilt());

        try
        {
            SearchResultList<T> result = Find(searchString);
            return Result.Success<SearchResultList<T>, SearchEngineError>(result);
        }
        catch (Exception ex)
        {
            return Result.Failure<SearchResultList<T>, SearchEngineError>(
                SearchEngineError.InternalError(ex.Message));
        }
    }
}
