// Ignore Spelling: Fusy

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
  public void IndexBuilderFromText_ShouldBuildIndexCorrectly()
  {
    var search = new TestSearch<int>();
    var sources = new string[3]
    {
      "1;Привет, как дела?",
      "2;Привет, как твои дела?",
      "3;Здравствуйте!"
    };

    var indexBuilder = new Search<int>.IndexBuilder(search);

    indexBuilder.BuildIndex(sources, ";");

    Assert.True(search.IsIndexComplete);
    Assert.NotNull(search.SearchIndex);
    Assert.Equal(5, search.SearchIndex.Count);
  }

  [Fact]
  public void IndexBuilderFromTuple_ShouldBuildIndexCorrectly()
  {
    var search = new TestSearch<int>();
    var sources = new List<(string, int)>()
    {
      ("Привет, как дела?", 1),
      ("Привет, как твои дела?", 2),
      ("Здравствуйте!", 3)
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

  [Fact]
  public void BuildIndex_WithEmptySources_ShouldNotAddAnyItems()
  {
    var search = new TestSearch<int>();
    var indexBuilder = new Search<int>.IndexBuilder(search);

    indexBuilder.BuildIndex(Enumerable.Empty<ISourceData<int>>());

    Assert.True(search.IsIndexComplete);
    Assert.Empty(search.SearchIndex);
  }

  [Fact]
  public void BuildIndex_WithEmptyStringsInArray_ShouldIgnoreEmptyStrings()
  {
    var search = new TestSearch<int>();
    var sources = new string[]
    {
        "1;Привет, как дела?",
        "2;;", // Пустая строка
        "3;Здравствуйте!"
    };

    var indexBuilder = new Search<int>.IndexBuilder(search);

    indexBuilder.BuildIndex(sources, ";");

    Assert.True(search.IsIndexComplete);
    Assert.Equal(4, search.SearchIndex.Count); // Только "Привет", "как", "дела", "Здравствуйте"
  }

  [Fact]
  public void BuildIndex_WithCustomDelimiters_ShouldSplitTextCorrectly()
  {
    var search = new TestSearch<int>();
    var sources = new List<ISourceData<int>>
    {
        new Test<int> { Id = 1, Text = "Привет-как-дела?" },
        new Test<int> { Id = 2, Text = "Привет-как-твои-дела?" },
        new Test<int> { Id = 3, Text = "Здравствуйте!" }
    };

    var indexBuilder = new Search<int>.IndexBuilder(search, delimiters: "-");

    indexBuilder.BuildIndex(sources);

    Assert.True(search.IsIndexComplete);
    Assert.Equal(5, search.SearchIndex.Count); // "Привет", "как", "дела", "твои", "Здравствуйте"
  }

  [Fact]
  public void BuildIndex_WithForceParallel_ShouldProcessInParallel()
  {
    var search = new TestSearch<int>()
    {
      IsNumberSearch = true
    };
    var sources = Enumerable.Range(1, 1000)
        .Select(i => new Test<int> { Id = i, Text = $"Text{i}" });

    var indexBuilder = new Search<int>.IndexBuilder(search, parallelProcessingThreshold: 100);

    indexBuilder.BuildIndex(sources);

    Assert.True(search.IsIndexComplete);
    Assert.Equal(1000, search.SearchIndex.Count);
  }

  [Fact]
  public void BuildIndex_WithPhoneticSearch_ShouldConvertTextToPhonetic()
  {
    var search = new TestSearch<int>(true);
    var sources = new List<ISourceData<int>>
    {
        new Test<int> { Id = 1, Text = "Привет" },
        new Test<int> { Id = 2, Text = "Превет" }, // Опечатка
        new Test<int> { Id = 3, Text = "Здравствуйте" }
    };

    var indexBuilder = new Search<int>.IndexBuilder(search);

    indexBuilder.BuildIndex(sources);

    Assert.True(search.IsIndexComplete);
    Assert.Equal(2, search.SearchIndex.Count); // "Привет" и "Здравствуйте" (фонетически)
  }

  [Fact]
  public void BuildIndex_WithNumberSearch_ShouldIncludeNumbers()
  {
    var search = new TestSearch<int> { IsNumberSearch = true };
    var sources = new List<ISourceData<int>>
    {
        new Test<int> { Id = 1, Text = "Text 123" },
        new Test<int> { Id = 2, Text = "Text 456" },
        new Test<int> { Id = 3, Text = "Text ABC" }
    };

    var indexBuilder = new Search<int>.IndexBuilder(search);

    indexBuilder.BuildIndex(sources);

    Assert.True(search.IsIndexComplete);
    Assert.Equal(4, search.SearchIndex.Count); // "Text", "123", "456", "ABC"
  }

  [Fact]
  public void BuildIndex_WithNullSources_ShouldThrowException()
  {
    var search = new TestSearch<int>();
    var indexBuilder = new Search<int>.IndexBuilder(search);

    Assert.Throws<ArgumentNullException>(() => indexBuilder.BuildIndex((IEnumerable<ISourceData<int>>)null!));
    Assert.Throws<ArgumentNullException>(() => indexBuilder.BuildIndex(null!, ";"));
    Assert.Throws<ArgumentNullException>(() => indexBuilder.BuildIndex((IEnumerable<(string, int)>)null!));
  }

  [Fact]
  public void BuildIndex_WithEmptyTuples_ShouldNotAddAnyItems()
  {
    var search = new TestSearch<int>();
    var indexBuilder = new Search<int>.IndexBuilder(search);

    indexBuilder.BuildIndex(Enumerable.Empty<(string, int)>());

    Assert.True(search.IsIndexComplete);
    Assert.Empty(search.SearchIndex);
  }

  [Fact]
  public void BuildIndex_WithCustomParallelThreshold_ShouldProcessCorrectly()
  {
    var search = new TestSearch<int>() { IsNumberSearch = true };
    var sources = Enumerable.Range(1, 100)
        .Select(i => new Test<int> { Id = i, Text = $"Text{i}" });

    var indexBuilder = new Search<int>.IndexBuilder(search, parallelProcessingThreshold: 50);

    indexBuilder.BuildIndex(sources);

    Assert.True(search.IsIndexComplete);
    Assert.Equal(100, search.SearchIndex.Count);
  }
}