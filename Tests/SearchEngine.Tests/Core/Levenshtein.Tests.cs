// Ignore Spelling: Levenshtein

namespace SearchEngine.Tests;

public class LevenshteinTests
{
  [Fact]
  public void Test_EmptyStrings()
  {
    Assert.Equal(0, Search<int>.Levenshtein.DistanceLevenhstein("", ""));
  }

  [Theory]
  [InlineData("ABC", 6)]
  [InlineData("A", 2)]
  [InlineData("HELLO", 10)]
  public void Test_SourceIsEmpty(string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenhstein("", target));
  }

  [Theory]
  [InlineData("ABC")]
  [InlineData("ПРИВЕТ")]
  [InlineData("TEST123")]
  public void Test_SameStrings(string str)
  {
    var sut = new Search<int>();

    Assert.Equal(0, Search<int>.Levenshtein.DistanceLevenhstein(str, str));
  }

  [Theory]
  [InlineData("KITTEN", "SITTING", 5)]
  [InlineData("SATURDAY", "SUNDAY", 6)]
  [InlineData("EXAMPLE", "SAMPLES", 4)]
  public void Test_DifferentStrings(string source, string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenhstein(source, target));
  }

  [Theory]
  [InlineData("HELLO", "HALLO", 2)]
  [InlineData("ABC", "ABD", 1)]
  [InlineData("ABCDEFG", "ABCDFG", 2)]
  public void Test_SubstitutionCost(string source, string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenhstein(source, target));
  }

  [Theory]
  [InlineData("HELLO", "HEO", 4)]
  [InlineData("ABCDEFG", "ABDEFGH", 3)]
  [InlineData("EXAMPLE", "EXMPLE", 2)]
  public void Test_InsertionDeletionCost(string source, string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenhstein(source, target));
  }

  [Theory]
  [InlineData("ПРОВЕРКА", "ПРОВЕРЬКА", 2)] // русский язык
  [InlineData("СОЛНЦЕ", "СОЛНЫШКО", 5)] // русские символы
  public void Test_RussianSpecialCases(string source, string target, int expected)
  {
    Assert.Equal(expected, Search<int>.Levenshtein.DistanceLevenhstein(source, target));
  }
}