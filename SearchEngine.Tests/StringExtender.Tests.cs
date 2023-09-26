namespace SearchEngine.Tests;

public class StringExtenderTests
{
  [Fact]
  public void Call_GetCharSet_on_latin_char_set_Returns_LatinaCharSet()
  {
    const string _testString = "abcds";

    Assert.Equal(CharSet.LatinaCharSet, _testString.GetCharSet());
  }

  [Fact]
  public void Call_GetCharSet_on_cyrillic_char_set_Returns_CyrillicCharSet()
  {
    const string _testString = "абвгд";

    Assert.Equal(CharSet.CyrillicCharSet, _testString.GetCharSet());
  }

  [Fact]
  public void Call_GetCharSet_on_combine_char_set_Returns_CombineCharSet()
  {
    const string _testString = "abcабв";

    Assert.Equal(CharSet.CombineCharSet, _testString.GetCharSet());
  }

  [Fact]
  public void Call_GetCharSet_on_numeric_char_set_Returns_OtherCharSet()
  {
    const string _testString = "123";

    Assert.Equal(CharSet.OtherCharSet, _testString.GetCharSet());
  }
}