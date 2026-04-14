using System.Collections;

namespace SearchEngine.Tests;

/// <summary>
/// Тесты перечисления источника при безопасной подготовке индекса.
/// </summary>
public class PrepareIndexResultSourceEnumerationTests
{
  [Fact]
  public async Task PrepareIndexResult_ДолженУспешноОбработатьОднопроходныйИсточник()
  {
    TestSearch<int> sut = new();
    SinglePassEnumerable<ISourceData<int>> source = new(
 [
        new TestSourceRecord { Id = 1, Text = "Красный велосипед" },
        new TestSourceRecord { Id = 2, Text = "Согласование договора" }
      ]);

    var result = await sut.PrepareIndexResult(source);

    Assert.True(result.IsSuccess);
    Assert.Equal(1, source.EnumerationCount);
    Assert.True(sut.IsIndexComplete);
    Assert.NotEmpty(sut.SearchIndex);
  }

  [Fact]
  public async Task PrepareIndexResult_ДолженВернутьОшибку_ЕслиИсточникБросилИсключениеПриПеречислении()
  {
    TestSearch<int> sut = new();
    ThrowingEnumerable<ISourceData<int>> source = new("Ошибка перечисления источника.");

    var result = await sut.PrepareIndexResult(source);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.IndexBuildFailed, result.Error!.Code);
    Assert.Equal("Во время подготовки поискового индекса произошла ошибка.", result.Error.Message);
    Assert.IsType<InvalidOperationException>(result.Error.Exception);
    Assert.Equal("Ошибка перечисления источника.", result.Error.Exception!.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task TryPrepareIndex_ДолженВернутьОшибкуIndexBuildFailed_ЕслиИсточникБросилИсключениеПриПеречислении()
  {
    TestSearch<int> sut = new();
    ThrowingEnumerable<ISourceData<int>> source = new("Ошибка перечисления источника.");

    var result = await sut.TryPrepareIndex(source);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.IndexBuildFailed, result.Error.Code);
    Assert.Equal(
    "Во время подготовки поискового индекса произошла ошибка. Ошибка перечисления источника.",
    result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  private sealed record TestSourceRecord : ISourceData<int>
  {
    public int Id { get; init; }

    public required string Text { get; init; }
  }

  private sealed class SinglePassEnumerable<T>(IEnumerable<T> items) : IEnumerable<T>
  {
    private readonly IEnumerable<T> _items = items;
    private bool _isEnumerated;

    public int EnumerationCount { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
      EnumerationCount++;

      if (_isEnumerated)
        throw new InvalidOperationException("Источник можно перечислить только один раз.");

      _isEnumerated = true;
      return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }

  private sealed class ThrowingEnumerable<T>(string message) : IEnumerable<T>
  {
    public IEnumerator<T> GetEnumerator() =>
throw new InvalidOperationException(message);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}