using ResultType;

namespace SearchEngine;

/// <summary>
/// Методы расширения для подготовки поискового индекса.
/// </summary>
public static partial class SearchExtension
{
  private const string _emptySourceErrorMessage = "Источник данных пуст или не содержит элементов.";
  private const string _indexBuildFailedMessage = "Во время подготовки поискового индекса произошла ошибка.";

  /// <summary>
  /// Подготавливает индекс из коллекции объектов-источников и возвращает результат операции.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Источник данных.</param>
  /// <param name="delimiters">
  /// Пользовательский набор разделителей слов. Если не задан, используется набор по умолчанию.
  /// </param>
  /// <returns>Успешный результат, если индекс подготовлен; иначе описание ошибки.</returns>
  /// <exception cref="ArgumentNullException">
  /// Генерируется, если <paramref name="search"/> равен <see langword="null"/>.
  /// </exception>
  public static Task<UnitResult<SearchError>> PrepareIndexResult<T>(
    this Search<T> search,
    IEnumerable<ISourceData<T>> source,
    string? delimiters = null)
    where T : struct
  {
    return search.PrepareIndexResult(
      source,
      delimiters,
      forceParallel: false,
      parallelProcessingThreshold: 10_000);
  }

  /// <summary>
  /// Подготавливает индекс из коллекции объектов-источников с настройками режима выполнения.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Источник данных.</param>
  /// <param name="forceParallel">
  /// <see langword="true"/>, если нужно принудительно использовать параллельную обработку.
  /// </param>
  /// <param name="parallelProcessingThreshold">
  /// Минимальный размер набора данных, начиная с которого допускается переход к параллельной обработке.
  /// </param>
  /// <returns>Успешный результат, если индекс подготовлен; иначе описание ошибки.</returns>
  /// <exception cref="ArgumentNullException">
  /// Генерируется, если <paramref name="search"/> равен <see langword="null"/>.
  /// </exception>
  public static Task<UnitResult<SearchError>> PrepareIndexResult<T>(
    this Search<T> search,
    IEnumerable<ISourceData<T>> source,
    bool forceParallel,
    int parallelProcessingThreshold = 10_000)
    where T : struct
  {
    return search.PrepareIndexResult(
      source,
      delimiters: null,
      forceParallel,
      parallelProcessingThreshold);
  }

  /// <summary>
  /// Подготавливает индекс из коллекции объектов-источников с настройками разделителей и режима выполнения.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Источник данных.</param>
  /// <param name="delimiters">
  /// Пользовательский набор разделителей слов. Если не задан, используется набор по умолчанию.
  /// </param>
  /// <param name="forceParallel">
  /// <see langword="true"/>, если нужно принудительно использовать параллельную обработку.
  /// </param>
  /// <param name="parallelProcessingThreshold">
  /// Минимальный размер набора данных, начиная с которого допускается переход к параллельной обработке.
  /// </param>
  /// <returns>Успешный результат, если индекс подготовлен; иначе описание ошибки.</returns>
  /// <exception cref="ArgumentNullException">
  /// Генерируется, если <paramref name="search"/> равен <see langword="null"/>.
  /// </exception>
  public static async Task<UnitResult<SearchError>> PrepareIndexResult<T>(
    this Search<T> search,
    IEnumerable<ISourceData<T>> source,
    string? delimiters,
    bool forceParallel,
    int parallelProcessingThreshold = 10_000)
    where T : struct
  {
    if (search is null)
      ThrowArgumentNull(nameof(search));

    try
    {
      ISourceData<T>[] materializedSource = MaterializeSource(source);

      if (materializedSource.Length == 0)
      {
        return UnitResult.Failure(
          new SearchError(
            SearchErrorCode.InvalidSourceRecord,
            _emptySourceErrorMessage))!;
      }

      search!.IndexPreparing();

      await Task
        .Run(() =>
        {
          Search<T>.IndexBuilder builder = new(
            search,
            delimiters,
            parallelProcessingThreshold);

          builder.BuildIndex(
            materializedSource,
            forceParallel);
        })
        .ConfigureAwait(false);

      return UnitResult.Success<SearchError>()!;
    }
    catch (Exception exception) when (!IsCriticalException(exception))
    {
      return UnitResult.Failure(
        new SearchError(
          SearchErrorCode.IndexBuildFailed,
          _indexBuildFailedMessage,
          exception))!;
    }
  }

  /// <summary>
  /// Подготавливает поисковый индекс по коллекции исходных данных.
  /// </summary>
  /// <param name="search">Экземпляр поискового движка.</param>
  /// <param name="source">Набор данных для построения индекса.</param>
  /// <param name="elementDelimiter"> Разделитель между идентификатором записи и индексируемым текстом в строке источника.</param>
  /// <param name="delimiters"> Пользовательский набор разделителей слов. Если не задан, используется набор по умолчанию.</param>
  /// <param name="forceParallel">
  /// <see langword="true"/>, если нужно принудительно использовать параллельную обработку.</param>
  /// <param name="parallelProcessingThreshold"> Минимальный размер набора данных, начиная с которого допускается переход к параллельной обработке.</param>
  /// <returns>
  /// Успешный результат либо описание ошибки подготовки индекса.
  /// </returns>
  /// <remarks>
  /// Метод не выбрасывает исключения в ожидаемых прикладных сценариях.
  /// Для новых интеграций рекомендуется использовать именно его.
  /// </remarks>
  public static async Task<UnitResult<SearchError>> PrepareIndexResult<T>(
    this Search<T> search,
    string[] source,
    string elementDelimiter,
    string? delimiters = null,
    bool forceParallel = false,
    int parallelProcessingThreshold = 10_000)
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

    if (!TryParseDelimitedSource(
          source,
          elementDelimiter,
          out List<(string Text, T Index)> parsedSource,
          out SearchError? error))
      return UnitResult.Failure(error!)!;

    try
    {
      search!.IndexPreparing();

      await Task.Run(() =>
      {
        Search<T>.IndexBuilder builder = new(search, delimiters, parallelProcessingThreshold);
        builder.BuildIndex(parsedSource, forceParallel);
      }).ConfigureAwait(false);

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
  /// Преобразует массив строковых записей в типизированный источник данных.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="source">Исходные строки.</param>
  /// <param name="elementDelimiter">Разделитель между идентификатором и текстом.</param>
  /// <param name="parsedSource">Результат успешного преобразования.</param>
  /// <param name="error">Описание ошибки, если преобразование не удалось.</param>
  /// <returns>
  /// <see langword="true"/>, если все строки успешно разобраны;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool TryParseDelimitedSource<T>(
    IEnumerable<string> source,
    string elementDelimiter,
    out List<(string Text, T Index)> parsedSource,
    out SearchError? error)
    where T : struct
  {
    parsedSource = new();
    int lineNumber = 0;

    foreach (string? sourceRecord in source)
    {
      lineNumber++;

      if (!TryParseDelimitedSourceRecord(
            sourceRecord,
            elementDelimiter,
            out (string Text, T Index) parsedRecord,
            out SearchError? itemError))
      {
        error = itemError! with
        {
          Message = $"Строка {lineNumber}: {itemError.Message}"
        };

        parsedSource.Clear();
        return false;
      }

      parsedSource.Add(parsedRecord);
    }

    error = null;
    return true;
  }

  /// <summary>
  /// Разбирает одну строку источника в пару идентификатор/текст.
  /// </summary>
  /// <typeparam name="T">Тип идентификатора записи.</typeparam>
  /// <param name="sourceRecord">Строка источника.</param>
  /// <param name="elementDelimiter">Разделитель между идентификатором и текстом.</param>
  /// <param name="parsedRecord">Результат успешного разбора.</param>
  /// <param name="error">Описание ошибки, если разбор не удался.</param>
  /// <returns>
  /// <see langword="true"/>, если строка успешно разобрана;
  /// иначе <see langword="false"/>.
  /// </returns>
  private static bool TryParseDelimitedSourceRecord<T>(
    string? sourceRecord,
    string elementDelimiter,
    out (string Text, T Index) parsedRecord,
    out SearchError? error)
    where T : struct
  {
    parsedRecord = (string.Empty, default);

    if (string.IsNullOrWhiteSpace(sourceRecord))
    {
      error = new SearchError(
        SearchErrorCode.InvalidSourceRecord,
        "Строка источника пуста.");
      return false;
    }

    int delimiterPosition = sourceRecord.IndexOf(elementDelimiter, StringComparison.Ordinal);

    if (delimiterPosition < 0)
    {
      error = new SearchError(
        SearchErrorCode.InvalidDelimitedSourceFormat,
        "В строке не найден разделитель между идентификатором и текстом.");
      return false;
    }

    string idPart = sourceRecord[..delimiterPosition].Trim();
    string textPart = sourceRecord[(delimiterPosition + elementDelimiter.Length)..].Trim();

    if (string.IsNullOrWhiteSpace(idPart))
    {
      error = new SearchError(
        SearchErrorCode.InvalidDelimitedSourceFormat,
        "Не указан идентификатор записи.");
      return false;
    }

    if (string.IsNullOrWhiteSpace(textPart))
    {
      error = new SearchError(
        SearchErrorCode.InvalidDelimitedSourceFormat,
        "Не указан текст записи.");
      return false;
    }

    if (!DefaultIdParser<T>.TryParse(idPart, out T id))
    {
      error = new SearchError(
        SearchErrorCode.InvalidIdFormat,
        $"Не удалось преобразовать идентификатор \"{idPart}\" к типу {typeof(T).Name}.");
      return false;
    }

    parsedRecord = (textPart, id);
    error = null;
    return true;
  }

     /// <summary>
  /// Материализует источник в массив, чтобы избежать повторного перечисления.
  /// </summary>
  /// <typeparam name="TItem">Тип элемента источника.</typeparam>
  /// <param name="source">Источник данных.</param>
  /// <returns>Материализованный массив элементов.</returns>
  private static TItem[] MaterializeSource<TItem>(IEnumerable<TItem>? source)
  {
    if (source is null)
      return [];

    if (source is TItem[] sourceArray)
      return sourceArray;

    return [.. source];
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