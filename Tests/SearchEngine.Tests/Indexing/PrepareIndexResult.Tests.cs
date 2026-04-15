namespace SearchEngine.Tests;

/// <summary>
/// Тесты безопасной подготовки индекса через Result.
/// </summary>
public class PrepareIndexResultTests
{
  [Fact]
  public async Task PrepareIndexResult_ДолженВернутьОшибку_ЕслиИсточникПуст()
  {
    TestSearch<int> sut = new();

    var result = await sut.PrepareIndexResult([]);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidSourceRecord, result.Error!.Code);
    Assert.Equal("Источник данных пуст или не содержит элементов.", result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task PrepareIndexResult_ДолженВернутьОшибку_ЕслиНеЗаданРазделительЭлементов()
  {
    TestSearch<int> sut = new();

    var result = await sut.PrepareIndexResult(Populating.GetTestPopulatedArray(), string.Empty);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.InvalidDelimitedSourceFormat, result.Error!.Code);
    Assert.Equal("Не задан разделитель между идентификатором и текстом.", result.Error.Message);
    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async Task PrepareIndexResult_ДолженВернутьУспех_ДляКоллекцииISourceData()
  {
    TestSearch<int> sut = new();

    var result = await sut.PrepareIndexResult(Populating.GetTestPopulatedList());

    Assert.True(result.IsSuccess);
    Assert.True(sut.IsIndexComplete);
    Assert.Equal(10, sut.SearchIndex.Count);
  }

  [Fact]
  public async Task PrepareIndexResult_ДолженВернутьУспех_ДляМассиваСтрок()
  {
    TestSearch<int> sut = new();

    var result = await sut.PrepareIndexResult(Populating.GetTestPopulatedArray(), ";");

    Assert.True(result.IsSuccess);
    Assert.True(sut.IsIndexComplete);
    Assert.Equal(10, sut.SearchIndex.Count);
  }
}