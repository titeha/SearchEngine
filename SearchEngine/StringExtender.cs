using System.Text;
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

  internal static string CorrectingString(string source)
  {
    StringBuilder _string = new(source);
    int i = 0;

    while (i < _string.Length)
      if (" ({[<.,)}]>;:!?".IsDelimiter(_string[i]))
        if (" .,)}]>!?:;".IsDelimiter(_string[i]) && 0 == i)
          _string.Remove(0, 1);
        else if (" .,:;!?".IsDelimiter(_string[i]) && "([{<".IsDelimiter(_string[i - 1]))
          _string.Remove(i, 1);
        else if (":;)]}>.,".IsDelimiter(_string[i]) && _string[i - 1] == ' ')
          _string.Remove(i - 1, 1);
        else if (i > 0 && _string[i] == _string[i - 1])
          _string.Remove(i - 1, 1);
        else
          i++;
      else
        i++;

    return _string.ToString();
  }

  internal static bool IsDelimiter(this string delimiters, char delimiter) => delimiters.IndexOf(delimiter) >= 0;

  [GeneratedRegex("\\p{IsCyrillic}", RegexOptions.Compiled)]
  private static partial Regex IsCyrillicRegex();

  [GeneratedRegex("[\\p{IsBasicLatin}-[0-9]]", RegexOptions.Compiled)]
  private static partial Regex isLatinRegex();
}