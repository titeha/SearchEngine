namespace SearchEngine.Service;

/// <summary>
/// Перехватывает необработанные исключения и возвращает единый ответ об ошибке.
/// </summary>
/// <remarks>
/// Гарантирует, что любая непредвиденная ошибка превращается в ответ <c>500</c> в формате
/// <see cref="ApiError"/>, а детали исключения попадают только в журнал и не уходят клиенту.
/// </remarks>
internal sealed class ExceptionHandlingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<ExceptionHandlingMiddleware> _logger;

  /// <summary>
  /// Создаёт middleware обработки необработанных исключений.
  /// </summary>
  /// <param name="next">Следующий обработчик конвейера.</param>
  /// <param name="logger">Журнал событий.</param>
  public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  /// <summary>
  /// Обрабатывает HTTP-запрос.
  /// </summary>
  /// <param name="context">Контекст HTTP-запроса.</param>
  /// <returns>Задача обработки запроса.</returns>
  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      await _next(context).ConfigureAwait(false);
    }
    catch (Exception exception)
    {
      _logger.LogError(
          exception,
          "Необработанная ошибка при обработке запроса {Method} {Path}.",
          context.Request.Method,
          context.Request.Path);

      if (context.Response.HasStarted)
        throw;

      context.Response.Clear();
      context.Response.StatusCode = StatusCodes.Status500InternalServerError;

      await context.Response
          .WriteAsJsonAsync(new ApiError
          {
            Code = "InternalError",
            Message = "Внутренняя ошибка сервиса."
          })
          .ConfigureAwait(false);
    }
  }
}
