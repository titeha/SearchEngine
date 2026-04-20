using SearchEngine.Models;

namespace SearchEngine.Tests;

public class IndexListTests
{
  [Fact]
  public void Create_new_IndexList_Result_is_not_empty()
  {
    IndexList<int> testIndexList = new(1);
    const int expectedCountElements = 1;

    Assert.Equal(expectedCountElements, testIndexList.Count);
  }

  [Fact]
  public void Add_new_elements_in_IndexList_Collection_has_new_elements_and_sorted()
  {
    IndexList<int> testIndexList = new();

    testIndexList.TryAddValue(2);
    testIndexList.TryAddValue(1);
    testIndexList.TryAddValue(4);
    testIndexList.TryAddValue(1);
    testIndexList.TryAddValue(9);
    testIndexList.TryAddValue(5);

    const int expectedCountElements = 5;
    const string expectedStringResult = "1,2,4,5,9";

    Assert.Equal(expectedCountElements, testIndexList.Count);
    Assert.Equal(expectedStringResult, testIndexList.ToString());
  }

  [Fact]
  public void Union_two_lists_Returns_new_list()
  {
    IndexList<int> testIndexList = new();
    IndexList<int> otherIndexList = new();

    testIndexList.TryAddValue(2);
    testIndexList.TryAddValue(1);
    testIndexList.TryAddValue(4);
    testIndexList.TryAddValue(1);
    testIndexList.TryAddValue(9);
    testIndexList.TryAddValue(5);

    otherIndexList.TryAddValue(4);
    otherIndexList.TryAddValue(7);
    otherIndexList.TryAddValue(1);
    otherIndexList.TryAddValue(8);
    otherIndexList.TryAddValue(2);

    IndexList<int> resultIndexList = testIndexList.UnionIndexes(otherIndexList);
    const string expectedResultString = "1,2,4";
    const int expectedElementCount = 3;

    Assert.Equal(expectedElementCount, resultIndexList.Count);
    Assert.Equal(expectedResultString, resultIndexList.ToString());
  }

  [Fact]
  public void Union_empty_list_with_list_contains_elements_Returns_empty_list()
  {
    IndexList<int> testIndexList = new();
    IndexList<int> emptyIndexList = new();

    testIndexList.TryAddValue(7);
    testIndexList.TryAddValue(2);
    testIndexList.TryAddValue(14);
    const string expectedResultString = "";
    const int expectedElementCount = 0;
    IndexList<int> resultIndexList = testIndexList.UnionIndexes(emptyIndexList);

    Assert.Equal(expectedElementCount, resultIndexList.Count);
    Assert.Equal(expectedResultString, resultIndexList.ToString());
  }

  [Fact]
  public void Union_list_with_collection_Result_has_all_indexes()
  {
    IndexList<int> testIndexList = new();
    List<IndexList<int>> addedIndexes = new()
    {
      new IndexList<int>(),
      new IndexList<int>()
    };

    testIndexList.TryAddValue(1);
    testIndexList.TryAddValue(3);
    testIndexList.TryAddValue(5);
    testIndexList.TryAddValue(7);
    addedIndexes[0].TryAddValue(2);
    addedIndexes[0].TryAddValue(4);
    addedIndexes[0].TryAddValue(5);
    addedIndexes[0].TryAddValue(9);
    addedIndexes[1].TryAddValue(7);
    addedIndexes[1].TryAddValue(15);
    addedIndexes[1].TryAddValue(27);
    addedIndexes[1].TryAddValue(115);
    const int expectedElementCount = 2;
    const string expectedResultString = "5,7";

    IndexList<int> resultIndexes = testIndexList.UnionIndexes(addedIndexes);

    Assert.Equal(expectedElementCount, resultIndexes.Count);
    Assert.Equal(expectedResultString, resultIndexes.ToString());
  }

  [Fact]
  public void IndexList_lookup_value_using_Contains_method_with_present_value_Return_true()
  {
    IndexList<int> testIndexList = new();
    IndexList<int> lookUpIndexList = new();

    testIndexList.TryAddValue(1);
    testIndexList.TryAddValue(2);
    testIndexList.TryAddValue(10);
    lookUpIndexList.TryAddValue(2);

    var result = testIndexList.Contains(lookUpIndexList);

    Assert.True(result);
  }

  [Fact]
  public void IndexList_lookup_value_using_Contains_method_without_present_value_Return_false()
  {
    IndexList<int> testIndexList = new();
    IndexList<int> lookUpIndexList = new();

    testIndexList.TryAddValue(1);
    testIndexList.TryAddValue(2);
    testIndexList.TryAddValue(10);
    lookUpIndexList.TryAddValue(7);

    var result = testIndexList.Contains(lookUpIndexList);

    Assert.False(result);
  }
}