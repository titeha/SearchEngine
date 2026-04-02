namespace SearchEngine.Tests;

public class DefaultIdParserTests
{
  private readonly record struct UnsupportedId(DateTime Value);

  [Theory]
  [InlineData("0", 0)]
  [InlineData("42", 42)]
  [InlineData("-17", -17)]
  public void Int_parser_parses_valid_values(string source, int expected)
  {
    bool ok = DefaultIdParser<int>.TryParse(source, out int actual);

    Assert.True(ok);
    Assert.Equal(expected, actual);
  }

  [Theory]
  [InlineData("9223372036854775807", long.MaxValue)]
  [InlineData("-9223372036854775808", long.MinValue)]
  public void Long_parser_parses_valid_values(string source, long expected)
  {
    bool ok = DefaultIdParser<long>.TryParse(source, out long actual);

    Assert.True(ok);
    Assert.Equal(expected, actual);
  }

  [Fact]
  public void Guid_parser_parses_valid_guid()
  {
    Guid expected = Guid.NewGuid();

    bool ok = DefaultIdParser<Guid>.TryParse(expected.ToString(), out Guid actual);

    Assert.True(ok);
    Assert.Equal(expected, actual);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("not-a-number")]
  [InlineData("12.5")]
  public void Int_parser_returns_false_for_invalid_input(string? source)
  {
    bool ok = DefaultIdParser<int>.TryParse(source, out int actual);

    Assert.False(ok);
    Assert.Equal(default, actual);
  }

  [Fact]
  public void Guid_parser_returns_false_for_invalid_input()
  {
    bool ok = DefaultIdParser<Guid>.TryParse("not-a-guid", out Guid actual);

    Assert.False(ok);
    Assert.Equal(Guid.Empty, actual);
  }

  [Fact]
  public void Unsupported_type_returns_false()
  {
    bool ok = DefaultIdParser<UnsupportedId>.TryParse("123", out UnsupportedId actual);

    Assert.False(ok);
    Assert.Equal(default, actual);
  }
}
