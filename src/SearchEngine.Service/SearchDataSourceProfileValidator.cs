namespace SearchEngine.Service;

/// <summary>
/// Проверяет профиль источника данных перед чтением документов.
/// </summary>
public sealed class SearchDataSourceProfileValidator
{
  private readonly SearchDataSourceReaderRegistry _registry;

  /// <summary>
  /// Создаёт валидатор профилей источников данных.
  /// </summary>
  /// <param name="registry">Registry reader-ов источников данных.</param>
  public SearchDataSourceProfileValidator(SearchDataSourceReaderRegistry registry)
  {
    _registry = registry;
  }

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

    string provider = source.Provider.Trim();

    ISearchDataSourceReader? reader = _registry.GetReader(provider);
    if (reader is null)
      return new ApiError
      {
        Code = "DataSourceProviderNotSupported",
        Message = $"Provider источника данных не поддерживается: {provider}."
      };

    return reader.ValidateProfile(sourceName, source);
  }
}
