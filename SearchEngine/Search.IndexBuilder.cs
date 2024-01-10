using System.Reflection;
using System.Text.RegularExpressions;

namespace SearchEngine;

public partial class Search<T> where T : struct
{
  internal partial class IndexBuilder
  {
    #region Константа
    public const string Delimiters = ".,()-:;!?\"\\'$_=[]<>/«»“” …’";
    #endregion

    #region Поля
    private readonly char[] _delimiters;

    private readonly Search<T> _search;

    private bool _isManualCreate;

    private readonly Regex _hasNumberAndCharacter = HasNumberAndCharacter();
    #endregion

    #region Конструкторы
    public IndexBuilder(Search<T> search, string? delimiters = null)
    {
      _search = search;
      _delimiters = (delimiters ?? Delimiters).ToCharArray();
    }
    #endregion

    #region Методы
    public void AddToIndex(string word, T index)
    {
      if (_isManualCreate)
        ProcessingLine(word, index);
    }

    public void BeginCreateIndex() => _isManualCreate = true;

    public void BuildIndex(IEnumerable<ISourceData<T>> sources)
    {
      if (_isManualCreate)
        return;

      var result = sources.AsParallel()
                          .WithDegreeOfParallelism(Environment.ProcessorCount)
                          .Select(s => (s.Id, TextList: s.Text.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries)))
                          .SelectMany(m => m.TextList, (m, v) => (Text: v.ToUpper(), m.Id))
                          .Where(c => c.Text.Length > 1 && (!_hasNumberAndCharacter.IsMatch(c.Text) || _search.IsNumberSearch))
                          .AsSequential()
                          .GroupBy(i => i.Text)
                          .Select(g => (Text: g.Key, Indexes: g.Select(r => r.Id).Distinct()))
                          .OrderBy(o => o.Text);

      if (_search.IsPhoneticSearch)
        result = ConvertToPhonetic(result);

      foreach (var (Text, Indexes) in result!)
        _search._searchIndex!.Add(Text, new(Indexes));

      _search.IsIndexComplete = true;
    }

    public void BuildIndex(string[] sources, string elementDelimiter)
    {
      if (_isManualCreate)
        return;

      var elementDelimiterArray = (elementDelimiter ?? ";").ToCharArray();
      var result = sources.AsParallel()
                          .WithDegreeOfParallelism(Environment.ProcessorCount)
                          .Select(s => s.Split(elementDelimiterArray, StringSplitOptions.RemoveEmptyEntries))
                          .Select(i => (Id: ConvertToId(i[0]), Text: i[1]))
                          .Select(d => (d.Id, TextList: d.Text.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries)))
                          .SelectMany(m => m.TextList, (m, v) => (Text: v.ToUpper(), m.Id))
                          .Where(c => c.Text.Length > 1 && (!_hasNumberAndCharacter.IsMatch(c.Text) || _search.IsNumberSearch))
                          .AsSequential()
                          .GroupBy(z => z.Text)
                          .Select(g => (Text: g.Key, Indexes: g.Select(f => f.Id).Distinct()))
                          .OrderBy(o => o.Text);

      if (_search.IsPhoneticSearch)
        result = ConvertToPhonetic(result);

      foreach (var (Text, Indexes) in result!)
        _search._searchIndex!.Add(Text, new(Indexes));

      _search.IsIndexComplete = true;
    }

    private static IOrderedEnumerable<(string Text, IEnumerable<T> Indexes)> ConvertToPhonetic(IOrderedEnumerable<(string Text, IEnumerable<T> Indexes)> result)
    {
      return result.Select(p => (Text: PhoneticSearch.MetaPhone(p.Text), p.Indexes))
                   .GroupBy(x => x.Text)
                   .Select(g => (Text: g.Key, Indexes: g.Select(t => t.Indexes)
                                                        .Aggregate((r, c) => r.Union(c))
                                                        .Distinct()))
                   .OrderBy(o => o.Text);
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

    [GeneratedRegex("(\\D+\\d+)|(\\d+\\D)|^\\d+$", RegexOptions.Compiled)]
    private static partial Regex HasNumberAndCharacter();
  }
}