namespace SearchEngine.Service;

/// <summary>
/// Проверяет профиль источника данных перед чтением документов.
/// </summary>
public sealed class SearchDataSourceProfileValidator
{
  /// <summary>
  /// Проверяет настройки профиля источника данных.
  /// </summary>
  /// <param name="sourceName">Имя источника данных.</param>
  /// <param name="source">Настройки источника данных.</param>
  /// <returns>Ошибка валидации или <see langword="null"/>, если профиль корректен.</returns>
  public ApiError? Validate(
      string sourceName,
      SearchDataSourceOptions source)
  {
    if (string.IsNullOrWhiteSpace(source.Provider))
      return new ApiError
      {
        Code = "DataSourceProviderIsEmpty",
        Message = $"Для источника данных не указан provider: {sourceName}."
      };

    if (string.Equals(
        source.Provider,
        SqliteSearchDataSourceReader.ProviderName,
        StringComparison.OrdinalIgnoreCase))
      return ValidateSqliteSource(sourceName, source);

    return null;
  }

  /// <summary>
  /// Проверяет настройки SQLite-источника данных.
  /// </summary>
  /// <param name="sourceName">Имя источника данных.</param>
  /// <param name="source">Настройки источника данных.</param>
  /// <returns>Ошибка валидации или <see langword="null"/>, если профиль корректен.</returns>
  private static ApiError? ValidateSqliteSource(
      string sourceName,
      SearchDataSourceOptions source)
  {
    if (string.IsNullOrWhiteSpace(source.ConnectionStringName))
      return new ApiError
      {
        Code = "DataSourceConnectionStringNameIsEmpty",
        Message = $"Для SQLite-источника не указано имя строки подключения: {sourceName}."
      };

    if (string.IsNullOrWhiteSpace(source.Query))
      return new ApiError
      {
        Code = "DataSourceQueryIsEmpty",
        Message = $"Для SQLite-источника не указан SQL-запрос: {sourceName}."
      };

    return null;
  }
}