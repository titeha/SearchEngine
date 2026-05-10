namespace SearchEngine.Service;

/// <summary>
/// Выполняет проверку работоспособности сервиса изнутри контейнера.
/// </summary>
internal static class HealthCheckCommand
{
  private const string _healthUrl = "http://127.0.0.1:8080/health";

  /// <summary>
  /// Проверяет, запрошен ли режим проверки работоспособности.
  /// </summary>
  /// <param name="args">Аргументы запуска приложения.</param>
  /// <returns><see langword="true"/>, если приложение запущено в режиме healthcheck.</returns>
  public static bool IsRequested(string[] args)
  {
    for (int i = 0; i < args.Length; i++)
      if (string.Equals(args[i], "--healthcheck", StringComparison.OrdinalIgnoreCase))
        return true;

    return false;
  }

  /// <summary>
  /// Выполняет HTTP-запрос к endpoint-у <c>/health</c>.
  /// </summary>
  /// <returns>Код выхода процесса: <c>0</c> при успехе, <c>1</c> при ошибке.</returns>
  public static async Task<int> RunAsync()
  {
    using HttpClient client = new()
    {
      Timeout = TimeSpan.FromSeconds(2)
    };

    try
    {
      using HttpResponseMessage response = await client
          .GetAsync(_healthUrl)
          .ConfigureAwait(false);

      return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch
    {
      return 1;
    }
  }
}