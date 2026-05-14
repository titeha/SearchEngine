using Microsoft.Extensions.Options;

namespace SearchEngine.Service;

/// <summary>
/// Восстанавливает поисковый индекс из snapshot-файла при старте сервиса.
/// </summary>
/// <remarks>
/// Создаёт сервис автоматического восстановления поискового индекса.
/// </remarks>
/// <param name="store">Хранилище поискового индекса.</param>
/// <param name="options">Настройки поискового сервиса.</param>
/// <param name="logger">Журнал событий сервиса.</param>
public sealed class SearchIndexRestoreHostedService(
    SearchIndexStore store,
    IOptions<SearchEngineServiceOptions> options,
    ILogger<SearchIndexRestoreHostedService> logger) : IHostedService
{
  private readonly SearchIndexStore _store = store;
  private readonly SearchEngineServiceOptions _options = options.Value;
  private readonly ILogger<SearchIndexRestoreHostedService> _logger = logger;

  /// <summary>
  /// Выполняет автоматическое восстановление индекса при старте сервиса.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Задача запуска фонового сервиса.</returns>
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    if (!_options.Snapshot.IsEnabled)
      return;

    if (!_options.Snapshot.AutoRestoreOnStart)
      return;

    ApiError? error = await _store
        .RestoreAsync(cancellationToken)
        .ConfigureAwait(false);

    if (error is null)
    {
      _logger.LogInformation("Поисковый индекс восстановлен из snapshot-файла.");
      return;
    }

    _logger.LogWarning(
        "Не удалось автоматически восстановить поисковый индекс из snapshot-файла. Код ошибки: {Code}. Сообщение: {Message}",
        [error.Code,
        error.Message]);
  }

  /// <summary>
  /// Останавливает фоновый сервис.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Завершённая задача остановки фонового сервиса.</returns>
  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}