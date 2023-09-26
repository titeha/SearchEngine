using CommonClasses;

namespace SearchEngine;

public static class SearchExtension
{
  public static async Task PrepareIndex<T>(this Search<T> search, IEnumerable<ISourceData<T>> source, string? delimiters = null) where T : struct
  {
    if (search == null)
      NullExceptionThrow(search);

    search!.PrepareIndex();

    if (source?.Any() != true)
      return;

    await Task.Run(() => new Search<T>.IndexBuilder(search, delimiters).BuildIndex(source));
  }

  public static async Task PrepareIndex<T>(this Search<T> search, string[] source, string elementDelimiter, string? delimiters = null) where T : struct
  {
    if (search == null)
      NullExceptionThrow(search);

    search!.PrepareIndex();

    if (source?.Any() != true)
      return;

    if (elementDelimiter.IsNullOrEmpty())
      return;

    await Task.Run(() => new Search<T>.IndexBuilder(search, delimiters).BuildIndex(source, elementDelimiter));
  }

  private static void NullExceptionThrow<T>(Search<T>? search) where T : struct => throw new ArgumentNullException(nameof(search));
}