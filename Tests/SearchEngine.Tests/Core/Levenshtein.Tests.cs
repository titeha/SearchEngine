// Ignore Spelling: Levenshtein

namespace SearchEngine.Tests;

public class LevenshteinTests
{
  [Fact]
  public void Test_EmptyStrings()
  {
    Assert.Equal(0, Search<int>.Levenshtein.DistanceLevenshtein("", ""));
  }

  [Theory]
  [InlineData("ABC", 6)]
  [InlineData("A", 2)]
  [InlineData("HELLO", 10)]
  public void Test_SourceIsEmpty(string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenshtein("", target));
  }

  [Theory]
  [InlineData("ABC")]
  [InlineData("ПРИВЕТ")]
  [InlineData("TEST123")]
  public void Test_SameStrings(string str)
  {
    Assert.Equal(0, Search<int>.Levenshtein.DistanceLevenshtein(str, str));
  }

  [Theory]
  [InlineData("KITTEN", "SITTING", 5)]
  [InlineData("SATURDAY", "SUNDAY", 6)]
  [InlineData("EXAMPLE", "SAMPLES", 4)]
  public void Test_DifferentStrings(string source, string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenshtein(source, target));
  }

  [Theory]
  [InlineData("HELLO", "HALLO", 2)]
  [InlineData("ABC", "ABD", 1)]
  [InlineData("ABCDEFG", "ABCDFG", 2)]
  public void Test_SubstitutionCost(string source, string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenshtein(source, target));
  }

  [Theory]
  [InlineData("HELLO", "HEO", 4)]
  [InlineData("ABCDEFG", "ABDEFGH", 3)]
  [InlineData("EXAMPLE", "EXMPLE", 2)]
  public void Test_InsertionDeletionCost(string source, string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenshtein(source, target));
  }

  [Theory]
  [InlineData("ПРОВЕРКА", "ПРОВЕРЬКА", 2)]
  [InlineData("СОЛНЦЕ", "СОЛНЫШКО", 5)]
  public void Test_RussianSpecialCases(string source, string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenshtein(source, target));
  }

  [Theory]
  [InlineData("KITTEN", "SITTING", 5)]
  [InlineData("SATURDAY", "SUNDAY", 6)]
  [InlineData("HELLO", "HALLO", 2)]
  [InlineData("ABCDEFG", "ABCDFG", 2)]
  public void Test_BoundedDistance_ReturnsExactDistance_WhenDistanceIsWithinThreshold(
    string source,
    string target,
    int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenshtein(source, target, expected));
  }

  [Fact]
  public void Test_BoundedDistance_ReturnsThresholdPlusOne_WhenDistanceIsGreaterThanThreshold()
  {
    Assert.Equal(3, Search<int>.Levenshtein.DistanceLevenshtein("ABCDEFGH", "ZZZZZZZZ", 2));
  }

  [Fact]
  public void Test_Distance_DoesNotThrow_WhenSymbolsAreUnknown()
  {
    Assert.Equal(2, Search<int>.Levenshtein.DistanceLevenshtein("☺", "☻"));
  }

  [Fact]
  public void Test_Distance_DoesNotThrow_WhenLowercaseSymbolsArePassedDirectly()
  {
    Assert.Equal(2, Search<int>.Levenshtein.DistanceLevenshtein("и", "о"));
  }
}
