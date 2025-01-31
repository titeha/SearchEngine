// Ignore Spelling: Fusy

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SearchEngine.Tests;

public class SearchTests
{
  [Fact]
  public async Task ExactSearch_ShouldFindExactMatch()
  {
    var search = new Search<int>();
    var sources = new List<ISourceData<int>>
    {
      new Test<int> { Id = 1, Text = "Привет, как дела?" },
      new Test<int> { Id = 2, Text = "Привет, как твои дела?" },
      new Test<int> { Id = 3, Text = "Здравствуйте!" }
    };

    await search.PrepareIndex(sources);

    var result = search.Find("Привет");

    Assert.Contains(1, result.Items[0].Items);
    Assert.Contains(2, result.Items[0].Items);
    Assert.DoesNotContain(3, result.Items[0].Items);
  }

  [Fact]
  public async Task FusySearch_ShouldFindWordsWithTypos()
  {
    var search = new Search<int>();
    var sources = new List<ISourceData<int>>
    {
        new Test<int> { Id = 1, Text = "Привет, как дела?" },
        new Test<int> { Id = 2, Text = "Привет, как твои дела?" },
        new Test<int> { Id = 3, Text = "Здравствуйте!" }
    };

    await search.PrepareIndex(sources);
    search.PrecisionSearch = 80; // Устанавливаем точность поиска
    search.SearchType = SearchType.NearSearch;

    var result = search.Find("Превет"); // Опечатка

    Assert.Contains(1, result.Items[2].Items);
    Assert.Contains(2, result.Items[2].Items);
    Assert.DoesNotContain(3, result.Items[2].Items);
  }

  [Fact]
  public async Task PhoneticFind_ShouldFindPhoneticallySimilarWords()
  {
    var search = new Search<int>(true);
    var sources = new List<ISourceData<int>>
    {
        new Test<int> { Id = 1, Text = "Привет, как дела?" },
        new Test<int> { Id = 2, Text = "Привет, как твои дела?" },
        new Test<int> { Id = 3, Text = "Здравствуйте!" }
    };

    await search.PrepareIndex(sources);

    var result = search.Find("Привед"); // Фонетически похоже на "Привет"

    Assert.Contains(1, result.Items[0].Items);
    Assert.Contains(2, result.Items[0].Items);
    Assert.DoesNotContain(3, result.Items[0].Items);
  }

  [Fact]
  public void IndexBuilder_ShouldBuildIndexCorrectly()
  {
    var search = new TestSearch<int>();
    var sources = new List<ISourceData<int>>
    {
        new Test<int> { Id = 1, Text = "Привет, как дела?" },
        new Test<int> { Id = 2, Text = "Привет, как твои дела?" },
        new Test<int> { Id = 3, Text = "Здравствуйте!" }
    };

    var indexBuilder = new Search<int>.IndexBuilder(search);

    indexBuilder.BuildIndex(sources);

    Assert.True(search.IsIndexComplete);
    Assert.NotNull(search.SearchIndex);
    Assert.Equal(5, search.SearchIndex.Count);
  }

  [Fact]
  public void PropertiesTest()
  {
    const int precision = 50;
    const int missprints = 2;

    var sut = new Search<int>();

    sut.PrecisionSearch = precision;
    sut.AcceptableCountMisprint = missprints;

    Assert.Equal(precision, sut.PrecisionSearch);
    Assert.Equal(missprints, sut.AcceptableCountMisprint);
  }

  [Fact]
  public void DisassemblyStringTest()
  {
    const string input = " hello world ";
    var expected = new SortedSet<string> { "HELLO", "WORLD" };
    var sut = new TestSearch<int>();

    sut.SearchDisassembling(input);

    Assert.Equal(expected, sut.SearchList);
  }

  [Fact]
  public async Task ExactSearchTest()
  {
    const string searchValue = "hello";
    var sut = new TestSearch<int>();
    var sources = new List<ISourceData<int>>
    {
      new Test<int> { Id = 1, Text = "Hello, here!" },
      new Test<int> { Id = 2, Text = "Hi!" },
      new Test<int> { Id = 3, Text = "Hello" }
    };
    await sut.PrepareIndex(sources);

    var result = sut.Find(searchValue);

    Assert.Single(result.Items);
    Assert.Contains(1, result.Items[0].Items);
    Assert.Contains(3, result.Items[0].Items);
  }

  [Fact]
  public void CreateIndexCompleteEventTest()
  {
    bool eventRaised = false;
    var sut = new TestSearch<int>();
    sut.CreateIndexComplete += (_, _) => eventRaised = true;

    sut.IsIndexComplete = true;

    Assert.True(eventRaised);
  }
}