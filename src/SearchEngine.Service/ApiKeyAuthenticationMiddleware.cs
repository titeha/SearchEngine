using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

namespace SearchEngine.Service;

/// <summary>
/// Проверяет API-ключ для запросов, изменяющих поисковый индекс.
/// </summary>
/// <remarks>
/// Защищаются только мутирующие endpoint-ы построения и восстановления индекса
/// (<c>POST /v1/index*</c>). Проверки работоспособности, состояние, поиск и справочники
/// остаются открытыми. Проверка применяется только когда задан ключ; иначе запрос проходит.
/// </remarks>
internal sealed class ApiKeyAuthenticationMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ApiKeyOptions _options;

  /// <summary>
  /// Создаёт middleware проверки API-ключа.
  /// </summary>
  /// <param name="next">Следующий обработчик конвейера.</param>
  /// <param name="options">Настройки поискового сервиса.</param>
  public ApiKeyAuthenticationMiddleware(RequestDelegate next, IOptions<SearchEngineServiceOptions> options)
  {
    _next = next;
    _options = options.Value.Authentication;
  }

  /// <summary>
  /// Обрабатывает HTTP-запрос.
  /// </summary>
  /// <param name="context">Контекст HTTP-запроса.</param>
  /// <returns>Задача обработки запроса.</returns>
  public async Task InvokeAsync(HttpContext context)
  {
    if (!RequiresApiKey(context.Request))
    {
      await _next(context).ConfigureAwait(false);
      return;
    }

    if (!IsAuthorized(context.Request))
    {
      context.Response.StatusCode = StatusCodes.Status401Unauthorized;

      await context.Response
          .WriteAsJsonAsync(new ApiError
          {
            Code = "Unauthorized",
            Message = "Требуется корректный API-ключ."
          })
          .ConfigureAwait(false);

      return;
    }

    await _next(context).ConfigureAwait(false);
  }

  /// <summary>
  /// Определяет, требует ли запрос проверки API-ключа.
  /// </summary>
  /// <param name="request">HTTP-запрос.</param>
  /// <returns><see langword="true"/>, если запрос нужно проверять.</returns>
  private bool RequiresApiKey(HttpRequest request)
  {
    if (!_options.IsEnabled || string.IsNullOrWhiteSpace(_options.ApiKey))
      return false;

    return HttpMethods.IsPost(request.Method)
        && request.Path.StartsWithSegments("/v1/index");
  }

  /// <summary>
  /// Проверяет, что запрос содержит корректный API-ключ.
  /// </summary>
  /// <param name="request">HTTP-запрос.</param>
  /// <returns><see langword="true"/>, если ключ корректен.</returns>
  private bool IsAuthorized(HttpRequest request)
  {
    if (!request.Headers.TryGetValue(_options.HeaderName, out Microsoft.Extensions.Primitives.StringValues providedKey))
      return false;

    string providedValue = providedKey.ToString();

    if (string.IsNullOrEmpty(providedValue))
      return false;

    byte[] providedBytes = Encoding.UTF8.GetBytes(providedValue);
    byte[] expectedBytes = Encoding.UTF8.GetBytes(_options.ApiKey);

    return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
  }
}
