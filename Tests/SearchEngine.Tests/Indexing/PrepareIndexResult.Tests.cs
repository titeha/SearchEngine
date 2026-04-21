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

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task PrepareIndexResult_ДляSourceDataСНастройкамиВыполнения_ДолженСтроитьИндекс(
  bool forceParallel)
  {
    // Arrange
    Search<int> sut = new();

    Test<int>[] source =
    [
      new() { Id = 1, Text = "договор поставки" },
    new() { Id = 2, Text = "акт выполненных работ" }
    ];

    // Act
    var prepareResult = await sut.PrepareIndexResult(
      source,
      forceParallel: forceParallel,
      parallelProcessingThreshold: 1);

    var findResult = sut.FindResult("договор");

    // Assert
    Assert.True(prepareResult.IsSuccess);
    Assert.True(findResult.IsSuccess);
    Assert.True(findResult.Value!.IsHasIndex);
  }
}