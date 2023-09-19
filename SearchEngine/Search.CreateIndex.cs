using System.Reflection;
using System.Text.RegularExpressions;

namespace SearchEngine;

public partial class Search<T> where T : struct
{
  public partial class CreateIndex
  {
    #region Константа
    public const string Delimiters = " .,()-:;!?\"\\'$_=[]<>/«»“” …";
    #endregion

    #region Поля
    private readonly char[] _delimiters;

    private readonly Search<T> _search;

    private bool _isManualCreate;

    private readonly Regex _hasNumberAndCharacter = HasNumberAndCharacter();
    #endregion

    #region Конструкторы
    public CreateIndex(Search<T> search) : this(search, Delimiters) { }

    public CreateIndex(Search<T> search, string delimiters)
    {
      _search = search;
      _delimiters = delimiters.ToCharArray();
      _search._searchIndex = new();
    }
    #endregion

    #region Методы
    public void AddToIndex(string word, T index) => ProcessingLine(word, index);

    public void BeginCreateIndex() => _isManualCreate = true;

    public void BuildIndex(IEnumerable<ISourceData<T>> sources, string? delimiterString = null)
    {
      var delimiterArray = (delimiterString ?? Delimiters).ToCharArray();
      var result = sources.AsParallel()
                          .Select(s => (s.Id, TextList: s.Text.Split(delimiterArray, StringSplitOptions.RemoveEmptyEntries)))
                          .SelectMany(m => m.TextList, (m, v) => (Text: v.ToUpper(), m.Id))
                          .GroupBy(i => i.Text)
                          .Select(g => (Text: g.Key, Indexes: g.Select(r => r.Id)))
                          .OrderBy(o => o.Text);

      foreach (var (Text, Indexes) in result)
        _search._searchIndex!.Add(Text, new(Indexes));
    }

    public void BuildIndex(string[] sources, string elementDelimiter, string? delimiterString = null)
    {
      var delimiterArray = (delimiterString ?? Delimiters).ToCharArray();
      var elementDelimiterArray = (elementDelimiter ?? ";").ToCharArray();
      var result = sources.AsParallel()
                          .Select(s => s.Split(elementDelimiterArray, StringSplitOptions.RemoveEmptyEntries))
                          .Select(i => (Id: ConvertToId(i[0]), Text: i[1]))
                          .Select(d => (d.Id, TextList: d.Text.Split(delimiterArray, StringSplitOptions.RemoveEmptyEntries)))
                          .SelectMany(m => m.TextList, (m, v) => (Text: v.ToUpper(), m.Id))
                          .GroupBy(z => z.Text)
                          .Select(g => (Text: g.Key, Indexes: g.Select(f => f.Id)))
                          .OrderBy(o => o.Text);

      foreach (var (Text, Indexes) in result)
        _search._searchIndex!.Add(Text, new(Indexes));
    }

    private T ConvertToId(string source)
    {
      Type type = typeof(T);

      if (type.IsPrimitive)
      {
        MethodInfo tryParseMethod = type.GetMethod("TryParse", new[] { typeof(string), type.MakeByRefType() })!;

        if (tryParseMethod != null)
        {
          object[] parameters = new object[] { source, null! };
          bool success = (bool)tryParseMethod.Invoke(null, parameters)!;

          if (success)
            return (T)parameters[1];
        }
      }

      return default;
    }

    public void EndCreateIndex() => _isManualCreate = false;

    private void ProcessingLine(string source, T index)
    {
      string[] sources = source.Trim().ToUpper().Split(_delimiters, StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0, _count = sources.Length; i < _count; i++)
        if (sources[i].Length > 1 && (_hasNumberAndCharacter.IsMatch(sources[i]) || _search.IsNumberSearch))
          AddWord(_search.IsPhoneticSearch ? PhoneticSearch.MetaPhone(sources[i]) : sources[i], index);
    }

    private void AddWord(string word, T index)
    {
      if (!_search._searchIndex!.TryAdd(word, new(index)))
        _search._searchIndex![word].TryAddValue(index);
    }
    #endregion

    #region Событие
    public event EventHandler<EventArgs>? IndexCreated;
    #endregion

    [GeneratedRegex("(\\D+\\d+)|(\\d+\\D)", RegexOptions.Compiled)]
    private static partial Regex HasNumberAndCharacter();
  }
}