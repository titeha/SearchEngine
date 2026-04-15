namespace SearchEngine;

/// <summary>
/// Описание ошибки, возникшей при работе поискового движка.
/// </summary>
/// <param name="Code">Код ошибки.</param>
/// <param name="Message">Текстовое описание ошибки.</param>
/// <param name="Exception">Исходное исключение, если оно доступно.</param>
public sealed record SearchError(
    SearchErrorCode Code,
    string Message,
    Exception? Exception = null);
