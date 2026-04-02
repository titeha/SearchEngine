namespace SearchEngine;

/// <summary>
/// Коды ошибок безопасного API поиска.
/// </summary>
public enum SearchEngineErrorCode
{
    NullSearchString,
    EmptySearchString,
    IndexNotBuilt,
    InternalError,
    SearchIsNull,
    SourceIsNull,
    SourceIsEmpty,
    ElementDelimiterIsEmpty,
    IndexBuildFailed
}

/// <summary>
/// Ошибка, возвращаемая безопасными методами библиотеки поиска.
/// </summary>
public readonly struct SearchEngineError
{
    public SearchEngineError(SearchEngineErrorCode code, string message)
    {
        Code = code;
        Message = message;
    }

    public SearchEngineErrorCode Code { get; }

    public string Message { get; }

    public override string ToString() => $"{Code}: {Message}";

    public static SearchEngineError NullSearchString() =>
        new(SearchEngineErrorCode.NullSearchString, "Поисковая строка не может быть null.");

    public static SearchEngineError EmptySearchString() =>
        new(SearchEngineErrorCode.EmptySearchString, "Поисковая строка пуста или состоит только из пробельных символов.");

    public static SearchEngineError IndexNotBuilt() =>
        new(SearchEngineErrorCode.IndexNotBuilt, "Индекс ещё не подготовлен. Выполните PrepareIndex перед поиском.");

    public static SearchEngineError InternalError(string message) =>
        new(SearchEngineErrorCode.InternalError, message);
}
