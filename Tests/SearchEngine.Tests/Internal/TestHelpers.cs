using SearchEngine.Models;

namespace SearchEngine.Tests;

public class TestSearch<T> : Search<T> where T : struct
{
  public TestSearch() { }

  public TestSearch(bool isPhoneticSearch) : base(isPhoneticSearch) { }

  public SortedList<string, IndexList<T>> SearchIndex => _searchIndex!;

  public SortedSet<string> SearchList => _searchList;

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

  public static IEnumerable<ISourceData<int>> GetTestPopulatedListWithCustomDelimiters()
  {
    return
    [
      new Test<int> { Id = 1, Text = "Check.the-process" },
      new Test<int> { Id = 2, Text = "Process.is,ready" },
      new Test<int> { Id = 3, Text = "Process,simple-work" },
      new Test<int> { Id = 4, Text = "Thread-number,is.123AB" },
      new Test<int> { Id = 5, Text = "The,date.is-19.09.2023" }
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

  public required string Text { get; init; } = string.Empty;
}