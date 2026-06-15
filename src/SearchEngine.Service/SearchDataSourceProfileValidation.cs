namespace SearchEngine.Service;

/// <summary>
/// Содержит общие проверки профилей источников данных.
/// </summary>
internal static class SearchDataSourceProfileValidation
{
  /// <summary>
  /// Проверяет профиль SQL-источника, который читает документы через заранее настроенную строку подключения и SQL-запрос.
  /// </summary>
  /// <param name="sourceName">Имя источника данных.</param>
  /// <param name="providerDisplayName">Отображаемое имя provider-а для сообщения об ошибке.</param>
  /// <param name="source">Настройки источника данных.</param>
  /// <returns>Ошибка валидации или <see langword="null"/>, если профиль корректен.</returns>
  public static ApiError? ValidateSqlQuerySource(
      string sourceName,
      string providerDisplayName,
      SearchDataSourceOptions source)
  {
    if (string.IsNullOrWhiteSpace(source.ConnectionStringName))
      return new ApiError
      {
        Code = "DataSourceConnectionStringNameIsEmpty",
        Message = $"Для {providerDisplayName}-источника не указано имя строки подключения: {sourceName}."
      };

    if (string.IsNullOrWhiteSpace(source.Query))
      return new ApiError
      {
        Code = "DataSourceQueryIsEmpty",
        Message = $"Для {providerDisplayName}-источника не указан SQL-запрос: {sourceName}."
      };

    return null;
  }
}
