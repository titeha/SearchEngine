using System.Text.RegularExpressions;

namespace SearchEngine;

internal static partial class StringExtender
{
  public static CharSet GetCharSet(this string source)
  {
    Regex _isCyrillic = IsCyrillicRegex();
    Regex _isLatin = isLatinRegex();

    bool _hasCyrillic = _isCyrillic.IsMatch(source);
    bool _hasLatin = _isLatin.IsMatch(source);

    return _hasCyrillic
        ? _hasLatin ? CharSet.CombineCharSet : CharSet.CyrillicCharSet
        : _hasLatin ? CharSet.LatinaCharSet : CharSet.OtherCharSet;
  }

  [GeneratedRegex("\\p{IsCyrillic}", RegexOptions.Compiled)]
  private static partial Regex IsCyrillicRegex();

  [GeneratedRegex("[\\p{IsBasicLatin}-[0-9]]", RegexOptions.Compiled)]
  private static partial Regex isLatinRegex();
}