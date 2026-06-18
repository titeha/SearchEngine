using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты middleware обработки необработанных исключений.
/// </summary>
public sealed class ExceptionHandlingMiddlewareTests
{
  /// <summary>
  /// Проверяет, что необработанное исключение превращается в ответ 500 с телом ApiError.
  /// </summary>
  [Fact]
  public async Task InvokeAsync_ПриИсключении_Возвращает500СApiError()
  {
    // Arrange
    RequestDelegate next = _ => throw new InvalidOperationException("boom");

    ExceptionHandlingMiddleware sut = new(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

    DefaultHttpContext context = new()
    {
      RequestServices = new ServiceCollection().BuildServiceProvider()
    };

    using MemoryStream body = new();
    context.Response.Body = body;

    // Act
    await sut.InvokeAsync(context);

    // Assert
    Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

    body.Position = 0;

    ApiError? error = await JsonSerializer.DeserializeAsync<ApiError>(
        body,
        new JsonSerializerOptions(JsonSerializerDefaults.Web));

    Assert.NotNull(error);
    Assert.Equal("InternalError", error.Code);
  }

  /// <summary>
  /// Проверяет, что без исключения управление передаётся дальше без изменения ответа.
  /// </summary>
  [Fact]
  public async Task InvokeAsync_БезИсключения_ПередаетУправлениеДальше()
  {
    // Arrange
    bool nextCalled = false;

    RequestDelegate next = _ =>
    {
      nextCalled = true;
      return Task.CompletedTask;
    };

    ExceptionHandlingMiddleware sut = new(next, NullLogger<ExceptionHandlingMiddleware>.Instance);

    DefaultHttpContext context = new();

    // Act
    await sut.InvokeAsync(context);

    // Assert
    Assert.True(nextCalled);
    Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
  }
}
