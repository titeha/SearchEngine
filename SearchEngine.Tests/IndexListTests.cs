namespace SearchEngine.Tests;

public class IndexListTests
{
  [Fact]
  public void Create_new_IndexList_Result_is_not_empty()
  {
    IndexList<int> _testIndexList = new(1);
    const int _expectedCountElements = 1;

    Assert.Equal(_expectedCountElements, _testIndexList.Count);
  }

  [Fact]
  public void Add_new_elements_in_IndexList_Collection_has_new_elements_and_sorted()
  {
    IndexList<int> _testIndexList = new();

    _testIndexList.TryAddValue(2);
    _testIndexList.TryAddValue(1);
    _testIndexList.TryAddValue(4);
    _testIndexList.TryAddValue(1);
    _testIndexList.TryAddValue(9);
    _testIndexList.TryAddValue(5);

    const int _expectedCountElements = 5;
    const string _expectedStringResult = "1,2,4,5,9";

    Assert.Equal(_expectedCountElements, _testIndexList.Count);
    Assert.Equal(_expectedStringResult, _testIndexList.ToString());
  }

  [Fact]
  public void Union_two_lists_Returns_new_list()
  {
    IndexList<int> _testIndexList = new();
    IndexList<int> _otherIndexList = new();

    _testIndexList.TryAddValue(2);
    _testIndexList.TryAddValue(1);
    _testIndexList.TryAddValue(4);
    _testIndexList.TryAddValue(1);
    _testIndexList.TryAddValue(9);
    _testIndexList.TryAddValue(5);

    _otherIndexList.TryAddValue(4);
    _otherIndexList.TryAddValue(7);
    _otherIndexList.TryAddValue(1);
    _otherIndexList.TryAddValue(8);
    _otherIndexList.TryAddValue(2);

    IndexList<int> _resultIndexList = _testIndexList.UnionIndexes(_otherIndexList);
    const string _expectedResultString = "1,2,4,5,7,8,9";
    const int _expectedElementCount = 7;

    Assert.Equal(_expectedElementCount, _resultIndexList.Count);
    Assert.Equal(_expectedResultString, _resultIndexList.ToString());
  }

  [Fact]
  public void Union_empty_list_with_list_contains_elements_Returns_list_like_senond_list()
  {
    IndexList<int> _testIndexList = new();
    IndexList<int> _emptyIndexList = new();

    _testIndexList.TryAddValue(7);
    _testIndexList.TryAddValue(2);
    _testIndexList.TryAddValue(14);
    const string _expectedResultString = "2,7,14";
    const int _expectedElementCount = 3;
    IndexList<int> _resultIndexList = _testIndexList.UnionIndexes(_emptyIndexList);

    Assert.Equal(_expectedElementCount, _resultIndexList.Count);
    Assert.Equal(_expectedResultString, _resultIndexList.ToString());
  }
}