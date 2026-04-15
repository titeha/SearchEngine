namespace SearchEngine;

/// <summary>
/// Описывает запись исходных данных, которая может быть проиндексирована поисковым движком.
/// </summary>
/// <typeparam name="T">Тип идентификатора записи.</typeparam>
public interface ISourceData<T> where T : struct
{
  /// <summary>
  /// Уникальный идентификатор записи.
  /// </summary>
  T Id { get; }

  /// <summary>
  /// Текст записи, по которому строится индекс и выполняется поиск.
  /// </summary>
  string Text { get; }
}
