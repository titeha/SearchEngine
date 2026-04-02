using ResultType;

namespace SearchEngine;

/// <summary>
/// Методы расширения для подготовки поискового индекса.
/// </summary>
public static class SearchExtension
{
  /// <summary>
  /// Подготавливает индекс из коллекции объектов-источников.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Источник данных.</param>
  /// <param name="delimiters">Пользовательский набор разделителей слов.</param>
  /// <returns>Асинхронная операция подготовки индекса.</returns>
  /// <exception cref="ArgumentNullException">
  /// Генерируется, если <paramref name="search"/> равен <see langword="null"/>.
  /// </exception>
  public static async Task PrepareIndex<T>(
      this Search<T> search,
      IEnumerable<ISourceData<T>> source,
      string? delimiters = null)
      where T : struct
  {
    if (search is null)
      ThrowArgumentNull(nameof(search));

    await search!.PrepareIndex(source, delimiters);

    if (source?.Any() != true)
      return;

    await Task.Run(() => new Search<T>.IndexBuilder(search!, delimiters).BuildIndex(source))
        .ConfigureAwait(false);
  }

  /// <summary>
  /// Подготавливает индекс из массива строк, содержащих идентификатор и текст.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Источник данных.</param>
  /// <param name="elementDelimiter">Разделитель между идентификатором и текстом.</param>
  /// <param name="delimiters">Пользовательский набор разделителей слов.</param>
  /// <returns>Асинхронная операция подготовки индекса.</returns>
  /// <exception cref="ArgumentNullException">
  /// Генерируется, если <paramref name="search"/> равен <see langword="null"/>.
  /// </exception>
  public static async Task PrepareIndex<T>(
      this Search<T> search,
      string[] source,
      string elementDelimiter,
      string? delimiters = null)
      where T : struct
  {
    if (search is null)
      ThrowArgumentNull(nameof(search));

    await search!.PrepareIndex(source, delimiters!);

    if (source?.Length == 0)
      return;

    if (string.IsNullOrWhiteSpace(elementDelimiter))
      return;

    await Task.Run(() => new Search<T>.IndexBuilder(search, delimiters).BuildIndex(source!, elementDelimiter))
        .ConfigureAwait(false);
  }

  /// <summary>
  /// Подготавливает индекс из коллекции объектов-источников и возвращает результат операции.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Источник данных.</param>
  /// <param name="delimiters">Пользовательский набор разделителей слов.</param>
  /// <returns>
  /// Успешный результат, если индекс подготовлен; иначе описание ошибки.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Генерируется, если <paramref name="search"/> равен <see langword="null"/>.
  /// </exception>
  public static async Task<UnitResult<SearchError>> PrepareIndexResult<T>(
      this Search<T> search,
      IEnumerable<ISourceData<T>> source,
      string? delimiters = null)
      where T : struct
  {
    if (search is null)
      ThrowArgumentNull(nameof(search));

    if (source?.Any() != true)
      return UnitResult.Failure(
                new SearchError(
                    SearchErrorCode.InvalidSourceRecord,
                    "Источник данных пуст или не содержит элементов."))!;

    try
    {
      await search!.PrepareIndex(source, delimiters).ConfigureAwait(false);
      return UnitResult.Success<SearchError>()!;
    }
    catch (Exception exception) when (!IsCriticalException(exception))
    {
      return UnitResult.Failure(
          new SearchError(
              SearchErrorCode.IndexBuildFailed,
              "Во время подготовки поискового индекса произошла ошибка.",
              exception))!;
    }
  }

  /// <summary>
  /// Подготавливает индекс из массива строк и возвращает результат операции.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Источник данных.</param>
  /// <param name="elementDelimiter">Разделитель между идентификатором и текстом.</param>
  /// <param name="delimiters">Пользовательский набор разделителей слов.</param>
  /// <returns>
  /// Успешный результат, если индекс подготовлен; иначе описание ошибки.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// Генерируется, если <paramref name="search"/> равен <see langword="null"/>.
  /// </exception>
  public static async Task<UnitResult<SearchError>> PrepareIndexResult<T>(
      this Search<T> search,
      string[] source,
      string elementDelimiter,
      string? delimiters = null)
      where T : struct
  {
    if (search is null)
      ThrowArgumentNull(nameof(search));

    if (source is null || source.Length == 0)
      return UnitResult.Failure(
                new SearchError(
                    SearchErrorCode.InvalidSourceRecord,
                    "Источник данных пуст или не содержит элементов."))!;

    if (string.IsNullOrWhiteSpace(elementDelimiter))
      return UnitResult.Failure(
                new SearchError(
                    SearchErrorCode.InvalidDelimitedSourceFormat,
                    "Не задан разделитель между идентификатором и текстом."))!;

    try
    {
      await search!.PrepareIndex(source, elementDelimiter, delimiters).ConfigureAwait(false);
      return UnitResult.Success<SearchError>()!;
    }
    catch (Exception exception) when (!IsCriticalException(exception))
    {
      return UnitResult.Failure(
          new SearchError(
              SearchErrorCode.IndexBuildFailed,
              "Во время подготовки поискового индекса произошла ошибка.",
              exception))!;
    }
  }

  /// <summary>
  /// Генерирует исключение о недопустимом значении аргумента.
  /// </summary>
  /// <param name="paramName">Имя параметра.</param>
  private static void ThrowArgumentNull(string paramName) =>
      throw new ArgumentNullException(paramName);

  /// <summary>
  /// Определяет, относится ли исключение к критическим.
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