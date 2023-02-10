using System.Text;

using CommonClasses;

namespace SearchEngine;

public static class PhoneticSearch
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

  internal static string MetaPhoneRus(string source)
  {
    StringBuilder _source = new StringBuilder(source.ToUpper()).Replace("Ь", null).Replace("Ъ", null);
    StringBuilder _resultString = new(_source.Length);
    char _current;

    if (_source.Length > 0)
    {
      if (_consonantRusPair.TryGetValue(_source[^1], out char b))
        _source[^1] = b;

      char _previous = ' ';

      for (int i = 0, _count = _source.Length; i < _count; i++)
      {
        _current = _source[i];

        if (_vovelsRusPair.TryGetValue(_current, out char s))
          if (('Й' == _previous || 'И' == _previous) && ('О' == _current || 'Е' == _current))
            _resultString[^1] = _previous = 'И';
          else
          {
            if (_previous != _current)
              _resultString.Append(s);
          }
        else if (_previous != _current)
        {
          if (_consonantRus.Contains(_current))
            if ('С' == _current && ('Д' == _previous || 'Т' == _previous))
              _resultString[^1] = _previous = 'Ц';
            else if (_consonantRusPair.TryGetValue(_previous, out char _s))
              _resultString[^1] = _previous = _s;
          _resultString.Append(_current);
        }

        _previous = _current;
      }
    }

    return _resultString.ToString();
  }

  internal static string MetaPhoneEng(string source)
  {
    StringBuilder _resultString = new(source.ToUpper());
    char _current;

    if (0 < _resultString.Length)
    {
      _current = _resultString[0];

      switch (_current)
      {
        case 'K':
        case 'G':
        case 'P':
          if (_resultString[1] == 'N')
            _resultString.Remove(0, 1);
          break;
        case 'A':
          if (_resultString[1] == 'E')
            _resultString.Remove(0, 1);
          break;
        case 'W':
          if (_resultString[1] == 'R')
            _resultString.Remove(0, 1);
          else if (_resultString[1] == 'H')
          {
            _resultString.Remove(1, 1);
            if (!_vovelsEng.Contains(_resultString[1]))
              _resultString.Remove(0, 1);
          }
          break;
        case 'X':
          _resultString[0] = 'S';
          break;
      }

      if ('B' == _resultString[^1] && 'M' == _resultString[^2])
        _resultString.Remove(_resultString.Length - 1, 1);

      char _previous = ' ';
      for (int i = 0; i < _resultString.Length; i++)
      {
        int _count = _resultString.Length;
        _current = _resultString[i];
        char _next = i < _count - 1 ? _resultString[i + 1] : ' ';
        char _second = i < _count - 2 ? _resultString[i + 2] : ' ';
        if (i > 0)
          _previous = _resultString[i - 1];
        if (_previous == _current && 'C' != _current)
          _resultString.Remove(i, 1);

        switch (_current)
        {
          case 'C':
            if ('I' == _next)
              if ('A' == _second)
                _resultString[i] = 'X';
              else
                _resultString[i] = 'S';
            else if ('H' == _next)
              if ('S' == _previous)
                _resultString[i] = 'K';
              else
                _resultString[i] = 'X';
            else if ('E' == _next || 'Y' == _next)
              _resultString[i] = 'S';
            else if ('K' == _next)
              _resultString.Remove(i, 1);
            else
              _resultString[i] = 'K';
            break;
          case 'D':
            if ('G' == _next)
            {
              if ('E' == _second || 'Y' == _second || 'I' == _second)
                _resultString[i] = 'J';
            }
            else
              _resultString[i] = 'T';
            break;
          case 'G':
            if ('H' == _next)
            {
              if (!_vovelsEng.Contains(_second) || i < _resultString.Length - 2)
                _resultString.Remove(i, 1);
            }
            else if ('N' == _next && i < _resultString.Length - 2 || _resultString.IndexOf("NED", _resultString.Length - 3) > 0)
              _resultString.Remove(i, 1);
            else if ('I' == _next || 'E' == _next || 'Y' == _next)
              _resultString[i] = 'J';
            else
              _resultString[i] = 'K';
            break;
          case 'H':
            if (_vovelsEng.Contains(_previous) && !_vovelsEng.Contains(_next))
              _resultString.Remove(i, 1);
            break;
          case 'P':
            if (_next == 'H')
            {
              _resultString[i] = 'F';
              _resultString.Remove(i + 1, 1);
            }
            break;
          case 'Q':
            _resultString[i] = 'K';
            break;
          case 'S':
            if (_next == 'H' || _next == 'I' && (_second == 'A' || _second == 'O'))
              _resultString[i] = 'X';
            break;
          case 'T':
            if (_next == 'I' && (_second == 'A' || _second == 'O'))
              _resultString[i] = 'X';
            else if (_next == 'H')
              _resultString.Replace("TH", "O", i, 2);
            else if (_next == 'C' && _second == 'H')
              _resultString.Remove(i, 1);
            break;
          case 'V':
            _resultString[i] = 'F';
            break;
          case 'X':
            _resultString[i] = 'K';
            _resultString.Insert(i + 1, 'S');
            break;
          case 'Y':
            if (_vovelsEng.Contains(_next))
              _resultString.Remove(i, 1);
            break;
          case 'Z':
            _resultString[i] = 'S';
            break;
        }
      }
    }

    return _resultString.ToString();
  }
}