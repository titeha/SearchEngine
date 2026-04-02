using StringFunctions;

namespace SearchEngine;

/// <summary>
/// Предоставляет методы расширения для подготовки поискового индекса в классе <see cref="Search{T}"/>.
/// </summary>
public static class SearchExtension
{
  /// <summary>
  /// Подготавливает поисковый индекс для указанных исходных данных.
  /// </summary>
  /// <typeparam name="T">Тип индекса. Должен быть типом значения.</typeparam>
  /// <param name="search">Экземпляр <see cref="Search{T}"/>, для которого подготавливается индекс.</param>
  /// <param name="source">Исходные данные для индексации.</param>
  /// <param name="delimiters">Разделители, используемые для разделения текста. Если null, используются разделители по умолчанию.</param>
  /// <param name="forceParallel">Принудительно использовать параллельную обработку. По умолчанию <c>false</c>.</param>
  /// <param name="parallelProcessingThreshold">Минимальное количество элементов для включения параллельной обработки. По умолчанию 10 000.</param>
  /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="search"/> равен null.</exception>
  /// <exception cref="ArgumentException">Выбрасывается, если <paramref name="source"/> равен null или пуст.</exception>
  public static async Task PrepareIndex<T>(
    this Search<T> search,
    IEnumerable<ISourceData<T>> source,
    string? delimiters = null,
    bool forceParallel = false,
    int parallelProcessingThreshold = 10_000) where T : struct => await PrepareIndexInternal(
      search,
      source,
      delimiters,
      parallelProcessingThreshold,
      (builder, src) => builder.BuildIndex(src, forceParallel));

  /// <summary>
  /// Подготавливает поисковый индекс для указанных исходных данных, представленных в виде массива строк.
  /// </summary>
  /// <typeparam name="T">Тип индекса. Должен быть типом значения.</typeparam>
  /// <param name="search">Экземпляр <see cref="Search{T}"/>, для которого подготавливается индекс.</param>
  /// <param name="source">Исходные данные для индексации, представленные в виде массива строк.</param>
  /// <param name="elementDelimiter">Разделитель, используемый для разделения каждой строки на элементы.</param>
  /// <param name="delimiters">Разделители, используемые для разделения текста. Если null, используются разделители по умолчанию.</param>
  /// <param name="forceParallel">Принудительно использовать параллельную обработку. По умолчанию <c>false</c>.</param>
  /// <param name="parallelProcessingThreshold">Минимальное количество элементов для включения параллельной обработки. По умолчанию 10 000.</param>
  /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="search"/> равен null.</exception>
  /// <exception cref="ArgumentException">Выбрасывается, если <paramref name="source"/> равен null, пуст или <paramref name="elementDelimiter"/> равен null или пуст.</exception>
  public static async Task PrepareIndex<T>(
    this Search<T> search,
    string[] source,
    string elementDelimiter,
    string? delimiters = null,
    bool forceParallel = false,
    int parallelProcessingThreshold = 10_000) where T : struct
  {
    if (elementDelimiter.IsNullOrEmpty())
      return;

    await PrepareIndexInternal(
      search,
      source,
      delimiters,
      parallelProcessingThreshold,
      (builder, src) => builder.BuildIndex(src, elementDelimiter, forceParallel));
  }

  /// <summary>
  /// Подготавливает поисковый индекс для указанных исходных данных, представленных в виде коллекции кортежей.
  /// </summary>
  /// <typeparam name="T">Тип индекса. Должен быть типом значения.</typeparam>
  /// <param name="search">Экземпляр <see cref="Search{T}"/>, для которого подготавливается индекс.</param>
  /// <param name="source">Исходные данные для индексации, представленные в виде коллекции кортежей.</param>
  /// <param name="delimiters">Разделители, используемые для разделения текста. Если null, используются разделители по умолчанию.</param>
  /// <param name="forceParallel">Принудительно использовать параллельную обработку. По умолчанию <c>false</c>.</param>
  /// <param name="parallelProcessingThreshold">Минимальное количество элементов для включения параллельной обработки. По умолчанию 10 000.</param>
  /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="search"/> равен null.</exception>
  /// <exception cref="ArgumentException">Выбрасывается, если <paramref name="source"/> равен null или пуст.</exception>
  public static async Task PrepareIndex<T>(
    this Search<T> search,
    IEnumerable<(string Text, T Index)> source,
    string? delimiters = null,
    bool forceParallel = false,
    int parallelProcessingThreshold = 10_000) where T : struct => await PrepareIndexInternal(
      search,
      source,
      delimiters,
      parallelProcessingThreshold,
      (builder, src) => builder.BuildIndex(src, forceParallel));

  /// <summary>
  /// Внутренний метод для обработки общей логики подготовки поискового индекса.
  /// </summary>
  /// <typeparam name="TInput">Тип исходных данных.</typeparam>
  /// <typeparam name="T">Тип индекса. Должен быть типом значения.</typeparam>
  /// <param name="search">Экземпляр <see cref="Search{T}"/>, для которого подготавливается индекс.</param>
  /// <param name="source">Исходные данные для индексации.</param>
  /// <param name="delimiters">Разделители, используемые для разделения текста. Если null, используются разделители по умолчанию.</param>
  /// <param name="forceParallel">Принудительно использовать параллельную обработку.</param>
  /// <param name="parallelProcessingThreshold">Минимальное количество элементов для включения параллельной обработки.</param>
  /// <param name="buildAction">Действие для построения индекса с использованием <see cref="Search{T}.IndexBuilder"/>.</param>
  /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="search"/> равен null.</exception>
  /// <exception cref="ArgumentException">Выбрасывается, если <paramref name="source"/> равен null или пуст.</exception>
  private static async Task PrepareIndexInternal<TInput, T>(
    Search<T> search,
    TInput source,
    string? delimiters,
    int parallelProcessingThreshold,
    Action<Search<T>.IndexBuilder, TInput> buildAction) where T : struct
  {
    if (search == null)
      NullExceptionThrow(search);

    search!.IndexPreparing();

    if (source == null || IsEmpty<TInput, T>(source))
      return;

    await Task.Run(() =>
    {
      var builder = new Search<T>.IndexBuilder(search, delimiters, parallelProcessingThreshold);
      buildAction(builder, source);
    }).ConfigureAwait(false);
  }

  /// <summary>
  /// Выбрасывает исключение <see cref="ArgumentNullException"/>, если <paramref name="search"/> равен null.
  /// </summary>
  /// <typeparam name="T">Тип индекса. Должен быть типом значения.</typeparam>
  /// <param name="search">Экземпляр <see cref="Search{T}"/> для проверки.</param>
  /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="search"/> равен null.</exception>
  private static void NullExceptionThrow<T>(Search<T>? search) where T : struct => throw new ArgumentNullException(nameof(search));

  /// <summary>
  /// Проверяет, являются ли исходные данные пустыми.
  /// </summary>
  /// <typeparam name="TInput">Тип исходных данных.</typeparam>
  /// <param name="source">Исходные данные для проверки.</param>
  /// <returns><c>true</c>, если исходные данные пусты; иначе <c>false</c>.</returns>
  private static bool IsEmpty<TInput, T>(TInput source) where T: struct
  {
    return source switch
    {
      IEnumerable<ISourceData<T>> enumerable => !enumerable.Any(),
      string[] array => array.Length == 0,
      IEnumerable<(string Text, int Index)> enumerable => !enumerable.Any(),
      _ => throw new NotSupportedException($"Не поддерживаемый тип: {typeof(TInput)}")
    };
  }
}