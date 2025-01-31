// Ignore Spelling: ienumerable

namespace SearchEngine.Tests;

public class CreateIndexTests
{
  [Fact]
  public async void Create_dictionary_from_ienumerable_ISourceData_IsNumericSearch_is_off_Search_has_full_dictionary()
  {
    TestSearch<int> sut = new();

    const int expectedCount = 10;

    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    Assert.Equal(expectedCount, sut.SearchIndex.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Create_dictionary_from_ienumerable_ISourceData_IsNumericSearch_is_on_Search_has_full_dictionary()
  {
    TestSearch<int> sut = new()
    {
      IsNumberSearch = true
    };
    const int expectedCount = 14;

    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    Assert.Equal(expectedCount, sut.SearchIndex.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Create_dictionary_from_string_array_IsPhoneticSearch_is_on_Search_has_full_dictionary()
  {
    TestSearch<int> sut = new(true);
    const int expectedCount = 5;

    await sut.PrepareIndex(Populating.GetTestPopulatedArrayForPhonetic(), ";");

    Assert.Equal(expectedCount, sut.SearchIndex.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Create_dictionary_from_ienumerable_ISourceData_IsPhoneticSearch_is_on_Search_has_full_dictionary()
  {
    TestSearch<int> sut = new(true);
    const int expectedCount = 5;

    await sut.PrepareIndex(Populating.GetTestPopulatedListForPhonetic());

    Assert.Equal(expectedCount, sut.SearchIndex.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Create_dictionary_from_string_array_IsNumericSearch_is_off_Search_has_full_dictionary()
  {
    TestSearch<int> sut = new();
    const int expectedCount = 10;

    await sut.PrepareIndex(Populating.GetTestPopulatedArray(), ";", " ");

    Assert.Equal(expectedCount, sut.SearchIndex.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Create_dictionary_from_string_array_IsNumericSearch_is_on_Search_has_full_directory()
  {
    TestSearch<int> sut = new()
    {
      IsNumberSearch = true
    };
    const int expectedCount = 14;

    await sut.PrepareIndex(Populating.GetTestPopulatedArray(), ";");

    Assert.Equal(expectedCount, sut.SearchIndex.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Try_create_index_on_null_object_Exception_throw()
  {
    TestSearch<int>? sut = null;

    await Assert.ThrowsAsync<ArgumentNullException>(() => sut!.PrepareIndex([]));
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut!.PrepareIndex(Array.Empty<string>(), string.Empty));
  }

  [Fact]
  public async void Try_create_index_with_empty_source_collection_Search_is_empty()
  {
    TestSearch<int> sut = new();

    await sut.PrepareIndex(Enumerable.Empty<ISourceData<int>>());

    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);

    await sut.PrepareIndex(new string[0], string.Empty);

    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }

  [Fact]
  public async void Try_create_index_with_empty_elementDelimiter_parameter_Search_is_empty()
  {
    TestSearch<int> sut = new();

    await sut.PrepareIndex(Populating.GetTestPopulatedArray(), string.Empty);

    Assert.False(sut.IsIndexComplete);
    Assert.Empty(sut.SearchIndex);
  }
}

public class TestSearch<T> : Search<T> where T : struct
{
  public TestSearch() { }

  public TestSearch(bool isPhoneticSearch) : base(isPhoneticSearch) { }

  public SortedList<string, IndexList<T>> SearchIndex => _searchIndex!;

  public SortedSet<string> SearchList=> _searchList!;

  public void SearchDisassembling(string source) => DisassemblyString(source);
}

internal static class Populating
{
  public static IEnumerable<ISourceData<int>> GetTestPopulatedList()
  {
    return
    [
      new Test<int> { Id = 1, Text = "Check the process" },
      new Test<int> { Id = 2, Text = "Process is ready" },
      new Test<int> { Id = 3, Text = "Process simple work" },
      new Test<int> { Id = 4, Text = "Thread number is 123AB" },
      new Test<int> { Id = 5, Text = "The date is 19.09.2023" }
    ];
  }

  public static string[] GetTestPopulatedArray()
  {
    return
    [
      "1;Check the process",
      "2;Process is ready",
      "3;Process simple work",
      "4;Thread number is 123AB",
      "5;The date is 19.09.2023"
    ];
  }

  public static IEnumerable<ISourceData<int>> GetTestPopulatedListForPhonetic()
  {
    return new List<ISourceData<int>>
    {
      new Test<int> { Id = 1, Text = "Фима" },
      new Test<int> { Id = 2, Text = "Егор" },
      new Test<int> { Id = 3, Text = "Забабонова" },
      new Test<int> { Id = 4, Text = "Гоголадзе" },
      new Test<int> { Id = 5, Text = "Терентьев" }
    };
  }

  public static string[] GetTestPopulatedArrayForPhonetic()
  {
    return
    [
      "1;Фима",
      "2;Егор",
      "3;Забабонова",
      "4;Гоголадзе",
      "5;Терентьев"
    ];
  }
}

public record Test<T> : ISourceData<T> where T : struct
{
  public T Id { get; init; }

  public required string Text { get; init; }
}