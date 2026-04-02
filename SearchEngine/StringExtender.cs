using System.Text.RegularExpressions;

namespace SearchEngine;

internal static partial class StringExtender
{
#if NET7_0_OR_GREATER
  [GeneratedRegex("\\p{IsCyrillic}", RegexOptions.Compiled)]
  private static partial Regex IsCyrillicRegex();

  [GeneratedRegex("[\\p{IsBasicLatin}-[0-9]]", RegexOptions.Compiled)]
  private static partial Regex IsLatinRegex();
#else
  private static readonly Regex _isCyrillicRegex = new(@"\p{IsCyrillic}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

  private static readonly Regex _isLatinRegex = new(@"[\p{IsBasicLatin}-[0-9]]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

  private static Regex IsCyrillicRegex() => _isCyrillicRegex;
  private static Regex IsLatinRegex() => _isLatinRegex;
#endif
  public static CharSet GetCharSet(this string source)
  {
    Regex isCyrillic = IsCyrillicRegex();
    Regex isLatin = IsLatinRegex();

    bool hasCyrillic = isCyrillic.IsMatch(source);
    bool hasLatin = isLatin.IsMatch(source);

    return hasCyrillic
        ? hasLatin ? CharSet.CombineCharSet : CharSet.CyrillicCharSet
        : hasLatin ? CharSet.LatinaCharSet : CharSet.OtherCharSet;
  }
}