namespace SearchEngine.Tests;

/// <summary>
/// Тесты совместимого безопасного API подготовки индекса.
/// </summary>
public class TryPrepareIndexTests
{
  [Fact]
  public async Task TryPrepareIndex_С_Null_ЭкземпляромПоиска_ДолженВернутьОшибкуSearchIsNull()
  {
    TestSearch<int>? sut = null;

    var result = await sut!.TryPrepareIndex(Populating.GetTestPopulatedList());

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.SearchIsNull, result.Error.Code);
    Assert.Equal("Экземпляр поиска не задан.", result.Error.Message);
  }

  [Fact]
  public async Task TryPrepareIndex_С_Null_Источником_ДолженВернутьОшибкуSourceIsNull()
  {
    TestSearch<int> sut = new();

    IEnumerable<ISourceData<int>>? source = null;

    var result = await sut.TryPrepareIndex(source);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.SourceIsNull, result.Error.Code);
    Assert.Equal("Источник данных не задан.", result.Error.Message);
  }

  [Fact]
  public async Task TryPrepareIndex_С_ПустымИсточником_ДолженВернутьОшибкуSourceIsEmpty()
  {
    TestSearch<int> sut = new();

    var result = await sut.TryPrepareIndex([]);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.SourceIsEmpty, result.Error.Code);
    Assert.Equal("Источник данных пуст или не содержит элементов.", result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task TryPrepareIndex_С_Null_МассивомСтрок_ДолженВернутьОшибкуSourceIsNull()
  {
    TestSearch<int> sut = new();

    string[]? source = null;

    var result = await sut.TryPrepareIndex(source, ";");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.SourceIsNull, result.Error.Code);
    Assert.Equal("Источник данных не задан.", result.Error.Message);
  }

  [Fact]
  public async Task TryPrepareIndex_С_ПустымРазделителем_ДолженВернутьОшибкуElementDelimiterIsEmpty()
  {
    TestSearch<int> sut = new();

    var result = await sut.TryPrepareIndex(Populating.GetTestPopulatedArray(), string.Empty);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.ElementDelimiterIsEmpty, result.Error.Code);
    Assert.Equal("Не задан разделитель между идентификатором и текстом.", result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task TryPrepareIndex_ДляКоллекцииISourceData_ДолженВернутьУспех()
  {
    TestSearch<int> sut = new();

    var result = await sut.TryPrepareIndex(Populating.GetTestPopulatedList());

    Assert.True(result.IsSuccess);
    Assert.True(sut.IsIndexComplete);
    Assert.Equal(10, sut.SearchIndex.Count);
  }

  [Fact]
  public async Task TryPrepareIndex_ДляМассиваСтрок_ДолженВернутьУспех()
  {
    TestSearch<int> sut = new();

    var result = await sut.TryPrepareIndex(Populating.GetTestPopulatedArray(), ";");

    Assert.True(result.IsSuccess);
    Assert.True(sut.IsIndexComplete);
    Assert.Equal(10, sut.SearchIndex.Count);
  }

  [Fact]
  public async Task TryPrepareIndex_С_БитойСтрокой_ДолженВернутьОшибкуIndexBuildFailed()
  {
    TestSearch<int> sut = new();

    var result = await sut.TryPrepareIndex(
      [
        "1 Check the process"
      ],
      ";");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.IndexBuildFailed, result.Error.Code);
    Assert.Equal(
      "Строка 1: В строке не найден разделитель между идентификатором и текстом.",
      result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task TryPrepareIndex_С_БитымИдентификатором_ДолженВернутьОшибкуIndexBuildFailed()
  {
    TestSearch<int> sut = new();

    var result = await sut.TryPrepareIndex(
      [
        "abc;Check the process"
      ],
      ";");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchEngineErrorCode.IndexBuildFailed, result.Error.Code);
    Assert.Equal(
      "Строка 1: Не удалось преобразовать идентификатор \"abc\" к типу Int32.",
      result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }
}