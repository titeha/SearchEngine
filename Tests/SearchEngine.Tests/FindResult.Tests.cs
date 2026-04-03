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

  [Fact]
  public async Task FindResult_ДолженВернутьОшибку_ЕслиВЗапросеНетПоисковыхСлов()
  {
    TestSearch<int> sut = new();
    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult("a i");

    Assert.True(result.IsFailure);
    Assert.Equal(SearchErrorCode.QueryHasNoSearchableTerms, result.Error!.Code);
    Assert.Equal("Поисковый запрос не содержит пригодных для поиска слов.", result.Error.Message);
  }

  [Fact]
  public async Task FindResult_ДолженВернутьРезультат_ЕслиПоискУспешен()
  {
    TestSearch<int> sut = new()
    {
      SearchType = SearchType.ExactSearch,
      SearchLocation = SearchLocation.BeginWord
    };

    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    var result = sut.FindResult("process");

    Assert.True(result.IsSuccess);
    Assert.True(result.Value!.IsHasIndex);
    Assert.True(result.Value.Items.ContainsKey(0));
    Assert.Equal("1,2,3", result.Value.Items[0].ToString());
  }
}