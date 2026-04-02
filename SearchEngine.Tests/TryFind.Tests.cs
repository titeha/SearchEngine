using FluentAssertions;
using ResultType;

namespace SearchEngine.Tests;

public class TryFindTests
{
    private sealed record SourceItem<T>(T Id, string Text) : ISourceData<T> where T : struct;

    [Fact]
    public void TryFind_With_Null_String_Returns_Failure()
    {
        var search = new Search<int>();

        Result<SearchResultList<int>, SearchEngineError> result = search.TryFind(null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SearchEngineErrorCode.NullSearchString);
    }

    [Fact]
    public void TryFind_With_Whitespace_String_Returns_Empty_Success()
    {
        var search = new Search<int>();

        Result<SearchResultList<int>, SearchEngineError> result = search.TryFind("   ");

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHasIndex.Should().BeFalse();
    }

    [Fact]
    public void TryFind_Without_Index_Returns_Failure()
    {
        var search = new Search<int>();

        Result<SearchResultList<int>, SearchEngineError> result = search.TryFind("APPLE");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SearchEngineErrorCode.IndexNotBuilt);
    }

    [Fact]
    public async Task TryFind_After_PrepareIndex_Returns_Success_Result()
    {
        var search = new Search<int>
        {
            SearchType = SearchType.ExactSearch
        };

        var data = new[]
        {
            new SourceItem<int>(1, "Apple route"),
            new SourceItem<int>(2, "Banana recipe"),
            new SourceItem<int>(3, "Cherry pie")
        };

        await search.PrepareIndex(data);

        Result<SearchResultList<int>, SearchEngineError> result = search.TryFind("APPLE");

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHasIndex.Should().BeTrue();
        result.Value[0].Items.Should().Contain(1);
    }
}
