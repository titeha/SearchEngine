using Microsoft.Extensions.Options;

namespace SearchEngine.Service;

/// <summary>
/// Отклоняет запросы, тело которых превышает допустимый размер.
/// </summary>
/// <remarks>
/// Проверяет заголовок <c>Content-Length</c> до чтения тела, поэтому слишком большой запрос
/// отклоняется без загрузки тела в память. Дополняет ограничение размера тела на уровне Kestrel
/// и работает в том числе за обратным прокси.
/// </remarks>
internal sealed class RequestBodySizeLimitMiddleware
{
  private readonly RequestDelegate _next;
  private readonly long _maxRequestBodyBytes;

  /// <summary>
  /// Создаёт middleware ограничения размера тела запроса.
  /// </summary>
  /// <param name="next">Следующий обработчик конвейера.</param>
  /// <param name="options">Настройки поискового сервиса.</param>
  public RequestBodySizeLimitMiddleware(RequestDelegate next, IOptions<SearchEngineServiceOptions> options)
  {
    _next = next;
    _maxRequestBodyBytes = options.Value.Limits.MaxRequestBodyBytes;
  }

  /// <summary>
  /// Обрабатывает HTTP-запрос.
  /// </summary>
  /// <param name="context">Контекст HTTP-запроса.</param>
  /// <returns>Задача обработки запроса.</returns>
  public async Task InvokeAsync(HttpContext context)
  {
    long? contentLength = context.Request.ContentLength;

    if (contentLength.HasValue && contentLength.Value > _maxRequestBodyBytes)
    {
      context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;

      await context.Response
          .WriteAsJsonAsync(new ApiError
          {
            Code = "RequestBodyTooLarge",
            Message = $"Размер тела запроса превышает допустимый: {_maxRequestBodyBytes} байт."
          })
          .ConfigureAwait(false);

      return;
    }

    await _next(context).ConfigureAwait(false);
  }
}
