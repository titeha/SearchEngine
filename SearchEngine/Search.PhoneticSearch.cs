using System.Text;

using CommonClasses;

namespace SearchEngine;

public partial class Search<T> where T : struct
{
  internal static class PhoneticSearch
  {
    #region Структуры для анализа слов
    private static readonly List<char> _consonantRus = new() { 'Т', 'С', 'В', 'К', 'Д', 'П', 'Г', 'З', 'Б', 'Ч', 'Х', 'Ж', 'Ш', 'Ц', 'Щ', 'Ф' }; // Буквы расставлены в порядке частоты появления

    private static readonly SortedList<char, char> _consonantRusPair = new()
  {
   {'Б','П' },
   {'В','Ф' },
   {'Г','К' },
   {'Д','Т' },
   {'З','C' }
  };

    private static readonly SortedList<char, char> _vovelsRusPair = new()
  {
   {'Е','И' },
   {'Ё','И' },
   {'О','А' },
   {'Ы','А' },
   {'Э','И' },
   {'Ю','У' },
   {'Я','А' }
  };

    private static readonly List<char> _vovelsEng = new() { 'E', 'A', 'O', 'I', 'U', 'Y' }; // Буквы расставлены в порядке частоты появления
    #endregion

    internal static string MetaPhone(string source) => source.GetCharSet() switch
    {
      CharSet.CyrillicCharSet => MetaPhoneRus(source),
      CharSet.LatinaCharSet => MetaPhoneEng(source),
      _ => string.Empty
    };

    private static string MetaPhoneRus(string original)
    {
      StringBuilder source = new StringBuilder(original.ToUpper()).Replace("Ь", null).Replace("Ъ", null);
      StringBuilder resultString = new(source.Length);
      char current;

      if (source.Length > 0)
      {
        if (_consonantRusPair.TryGetValue(source[^1], out char b))
          source[^1] = b;

        char previous = ' ';

        for (int i = 0, count = source.Length; i < count; i++)
        {
          current = source[i];

          if (_vovelsRusPair.TryGetValue(current, out char s))
            if (('Й' == previous || 'И' == previous) && ('О' == current || 'Е' == current))
              resultString[^1] = previous = 'И';
            else
            {
              if (previous != current)
                resultString.Append(s);
            }
          else if (previous != current)
          {
            if (_consonantRus.Contains(current))
              if ('С' == current && ('Д' == previous || 'Т' == previous))
                resultString[^1] = previous = 'Ц';
              else if (_consonantRusPair.TryGetValue(previous, out char _s))
                resultString[^1] = previous = _s;
            resultString.Append(current);
          }

          previous = current;
        }
      }

      return resultString.ToString();
    }

    private static string MetaPhoneEng(string source)
    {
      StringBuilder resultString = new(source.ToUpper());
      char current;

      if (0 < resultString.Length)
      {
        current = resultString[0];

        switch (current)
        {
          case 'K':
          case 'G':
          case 'P':
            if (resultString[1] == 'N')
              resultString.Remove(0, 1);
            break;
          case 'A':
            if (resultString[1] == 'E')
              resultString.Remove(0, 1);
            break;
          case 'W':
            if (resultString[1] == 'R')
              resultString.Remove(0, 1);
            else if (resultString[1] == 'H')
            {
              resultString.Remove(1, 1);
              if (!_vovelsEng.Contains(resultString[1]))
                resultString.Remove(0, 1);
            }
            break;
          case 'X':
            resultString[0] = 'S';
            break;
        }

        if ('B' == resultString[^1] && resultString[^2] == 'M')
          resultString.Remove(resultString.Length - 1, 1);

        char previous = ' ';
        for (int i = 0; i < resultString.Length; i++)
        {
          int _count = resultString.Length;
          current = resultString[i];
          char _next = i < _count - 1 ? resultString[i + 1] : ' ';
          char _second = i < _count - 2 ? resultString[i + 2] : ' ';
          if (i > 0)
            previous = resultString[i - 1];
          if (previous == current && current != 'C')
            resultString.Remove(i, 1);

          switch (current)
          {
            case 'C':
              if ('I' == _next)
                if ('A' == _second)
                  resultString[i] = 'X';
                else
                  resultString[i] = 'S';
              else if ('H' == _next)
                if ('S' == previous)
                  resultString[i] = 'K';
                else
                  resultString[i] = 'X';
              else if ('E' == _next || 'Y' == _next)
                resultString[i] = 'S';
              else if ('K' == _next)
                resultString.Remove(i, 1);
              else
                resultString[i] = 'K';
              break;
            case 'D':
              if ('G' == _next)
              {
                if ('E' == _second || 'Y' == _second || 'I' == _second)
                  resultString[i] = 'J';
              }
              else
                resultString[i] = 'T';
              break;
            case 'G':
              if ('H' == _next)
              {
                if (!_vovelsEng.Contains(_second) || i < resultString.Length - 2)
                  resultString.Remove(i, 1);
              }
              else if ('N' == _next && i < resultString.Length - 2 || resultString.IndexOf("NED", resultString.Length - 3) > 0)
                resultString.Remove(i, 1);
              else if ('I' == _next || 'E' == _next || 'Y' == _next)
                resultString[i] = 'J';
              else
                resultString[i] = 'K';
              break;
            case 'H':
              if (_vovelsEng.Contains(previous) && !_vovelsEng.Contains(_next))
                resultString.Remove(i, 1);
              break;
            case 'P':
              if (_next == 'H')
              {
                resultString[i] = 'F';
                resultString.Remove(i + 1, 1);
              }
              break;
            case 'Q':
              resultString[i] = 'K';
              break;
            case 'S':
              if (_next == 'H' || _next == 'I' && (_second == 'A' || _second == 'O'))
                resultString[i] = 'X';
              break;
            case 'T':
              if (_next == 'I' && (_second == 'A' || _second == 'O'))
                resultString[i] = 'X';
              else if (_next == 'H')
                resultString.Replace("TH", "O", i, 2);
              else if (_next == 'C' && _second == 'H')
                resultString.Remove(i, 1);
              break;
            case 'V':
              resultString[i] = 'F';
              break;
            case 'X':
              resultString[i] = 'K';
              resultString.Insert(i + 1, 'S');
              break;
            case 'Y':
              if (_vovelsEng.Contains(_next))
                resultString.Remove(i, 1);
              break;
            case 'Z':
              resultString[i] = 'S';
              break;
          }
        }
      }

      return resultString.ToString();
    }
  }
}