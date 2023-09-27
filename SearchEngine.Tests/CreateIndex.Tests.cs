using System.Dynamic;

namespace SearchEngine.Tests;

public class CreateIndexTests
{
  [Fact]
  public async void Create_dictionary_from_ienumerable_ISourceData_IsNumericSearch_is_off_Search_has_full_dictionary()
  {
    TestSearch<int> sut = new();

    const int expectedCount = 10;

    await sut.PrepareIndex(Populating.GetTestPopulatedList());

    Assert.Equal(expectedCount, sut.SearchList.Count);
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

    Assert.Equal(expectedCount, sut.SearchList.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Create_dictionary_from_ienumerable_ISourceData_IsPhoneticSearch_is_on_Search_has_full_dictionary()
  {
    TestSearch<int> sut = new(true);
    const int expectedCount = 5;

    await sut.PrepareIndex(Populating.GetTestPopulatedListForPhonetic());

    Assert.Equal(expectedCount, sut.SearchList.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Create_dictionary_from_string_array_IsNumericSearch_is_off_Search_has_full_dictionary()
  {
    TestSearch<int> sut = new();
    const int expectedCount = 10;

    await sut.PrepareIndex(Populating.GetTestPopulatedArray(), ";", " ");

    Assert.Equal(expectedCount, sut.SearchList.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Create_dictionary_from_string_array_IsNumericSearch_is_on_Search_has_full_directory()
  {
    TestSearch<int> sut = new();
    sut.IsNumberSearch = true;
    const int expectedCount = 14;

    await sut.PrepareIndex(Populating.GetTestPopulatedArray(), ";");

    Assert.Equal(expectedCount, sut.SearchList.Count);
    Assert.True(sut.IsIndexComplete);
  }

  [Fact]
  public async void Create_dictionary_from_string_array_IsPhoneticSearch_is_on_Search_has_full_dictionary()
  {
    TestSearch<int> sut = new(true);
    const int expectedCount = 5;

    await sut.PrepareIndex(Populating.GetTestPopulatedArrayForPhonetic(), ";");

    Assert.Equal(expectedCount,sut.SearchList.Count);
    Assert.True(sut.IsIndexComplete);
  }
}

public class TestSearch<T> : Search<T> where T : struct
{
  public TestSearch() : base() { }

  public TestSearch(bool isPhoneticSearch) : base(isPhoneticSearch) { }

  public SortedList<string, IndexList<T>> SearchList => _searchIndex!;
}

internal static class Populating
{
  public static IEnumerable<ISourceData<int>> GetTestPopulatedList()
  {
    return new List<ISourceData<int>>
    {
      new Test<int> { Id = 1, Text = "Check the process" },
      new Test<int> { Id = 2, Text = "Process is ready" },
      new Test<int> { Id = 3, Text = "Process simple work" },
      new Test<int> { Id = 4, Text = "Thread number is 123AB" },
      new Test<int> { Id = 5, Text = "The date is 19.09.2023" }
    };
  }

  public static string[] GetTestPopulatedArray()
  {
    return new string[]
    {
      "1;Check the process",
      "2;Process is ready",
      "3;Process simple work",
      "4;Thread number is 123AB",
      "5;The date is 19.09.2023"
    };
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
    return new string[]
    {
      "1;Фима",
      "2;Егор",
      "3;Забабонова",
      "4;Гоголадзе",
      "5;Терентьев"
    };
  }
}

public record Test<T> : ISourceData<T> where T : struct
{
  public T Id { get; init; }

  public required string Text { get; init; }
}