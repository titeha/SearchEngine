namespace SearchEngine.Tests;

public class FindResultTests
{
  [Fact]
  public void FindResult_ДолженВернутьОшибку_ЕслиЗапросПустой()
  {
    TestSearch<int> sut = new();

    var result = sut.FindResult(string.Empty);

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.EmptyQuery, result.Error!.Code);
    Assert.Equal("Поисковый запрос пуст.", result.Error.Message);
  }

  [Fact]
  public void FindResult_ДолженВернутьОшибку_ЕслиИндексНеПодготовлен()
  {
    TestSearch<int> sut = new();

    var result = sut.FindResult("process");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.IndexNotBuilt, result.Error!.Code);
    Assert.Equal("Поисковый индекс не подготовлен.", result.Error!.Message);
  }

  [Fact]
  public void FindResult_ДолженВернутьОшибку_ЕслиИндексПуст()
  {
    TestSearch<int> sut = new()
    {
      IsIndexComplete = true
    };

    var result = sut.FindResult("process");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.IndexIsEmpty, result.Error!.Code);
    Assert.Equal("Поисковый индекс пуст.", result.Error.Message);
  }
}