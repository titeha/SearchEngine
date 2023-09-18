using System.Text.RegularExpressions;

namespace SearchEngine;

internal static partial class StringExtender
{
  public static CharSet GetCharSet(this string source)
  {
    Regex isCyrillic = IsCyrillicRegex();
    Regex isLatin = isLatinRegex();

    bool hasCyrillic = isCyrillic.IsMatch(source);
    bool hasLatin = isLatin.IsMatch(source);

    return hasCyrillic
        ? hasLatin ? CharSet.CombineCharSet : CharSet.CyrillicCharSet
        : hasLatin ? CharSet.LatinaCharSet : CharSet.OtherCharSet;
  }

  [GeneratedRegex("\\p{IsCyrillic}", RegexOptions.Compiled)]
  private static partial Regex IsCyrillicRegex();

  [GeneratedRegex("[\\p{IsBasicLatin}-[0-9]]", RegexOptions.Compiled)]
  private static partial Regex isLatinRegex();
}