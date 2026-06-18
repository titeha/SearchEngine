namespace SearchEngine.Service;

/// <summary>
/// Lock-free шлюз «один в полёте».
/// </summary>
/// <remarks>
/// Гарантирует, что одновременно выполняется не более одной операции, но не блокирует
/// потоки и не выстраивает очередь ожидания: второй вызывающий сразу узнаёт, что операция
/// уже идёт, и не ждёт её завершения. Используется для сериализации построения одного
/// поискового индекса без локов.
/// </remarks>
internal sealed class SingleFlightGate
{
  private int _inProgress;

  /// <summary>
  /// Пытается занять шлюз.
  /// </summary>
  /// <returns><see langword="true"/>, если шлюз занят текущим вызывающим и операцию можно выполнять;
  /// <see langword="false"/>, если операция уже выполняется другим вызывающим.</returns>
  public bool TryEnter() => Interlocked.CompareExchange(ref _inProgress, 1, 0) == 0;

  /// <summary>
  /// Освобождает шлюз.
  /// </summary>
  public void Exit() => Volatile.Write(ref _inProgress, 0);

  /// <summary>
  /// Получает признак того, что операция сейчас выполняется.
  /// </summary>
  public bool IsInProgress => Volatile.Read(ref _inProgress) == 1;
}
