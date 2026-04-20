using System.Text.RegularExpressions;

using SearchEngine.Properties;

using StringFunctions;

namespace SearchEngine;

public partial class Search<T> where T : struct
{
  internal partial class IndexBuilder(Search<T> search, string? delimiters = null, int parallelProcessingThreshold = 10_000)
  {
    #region Константа
    public const string Delimiters = ".,()-:;!?\"\\'$_=[]<>/«»“” …’\t";
    #endregion

    #region Поля
    private readonly char[] _delimiters = (delimiters!.IsNullOrEmpty() ? Delimiters : delimiters)!.ToCharArray();

    private readonly Search<T> _search = search;

#if NET7_0_OR_GREATER
    [GeneratedRegex("(\\D+\\d+)|(\\d+\\D)|^\\d+$", RegexOptions.Compiled)]
    private static partial Regex HasNumberAndCharacter();
    private readonly Regex _hasNumberAndCharacter = HasNumberAndCharacter();
#else
    private static readonly Regex _hasNumberAndCharacter = new("(\\D+\\d+)|(\\d+\\D)|^\\d+$", RegexOptions.Compiled);
#endif

    private readonly int _parallelProcessingThreshold = parallelProcessingThreshold;
    #endregion

    #region Методы
    public void BuildIndex(IEnumerable<ISourceData<T>> sources, bool forceParallel = false)
    {
      var result = ProcessSource(
        sources,
        s => (s.Id, s.Text),
        s => s.Text,
        forceParallel);

      if (_search.IsPhoneticSearch)
        result = ConvertToPhonetic(result);

      AddToIndex(result);
      NotifyIndexCreated();
    }

    public void BuildIndex(string[] sources, string elementDelimiter, bool forceParallel = false)
    {
      string actualDelimiter = elementDelimiter.IsNullOrEmpty() ? ";" : elementDelimiter;

      var result = ProcessSource(
        sources,
        s =>
        {
          if (string.IsNullOrWhiteSpace(s))
            return null;

          int delimiterPosition = s.IndexOf(actualDelimiter, StringComparison.Ordinal);
          if (delimiterPosition < 0)
            return null;

          string idPart = s[..delimiterPosition].Trim();
          string textPart = s[(delimiterPosition + actualDelimiter.Length)..].Trim();

          if (string.IsNullOrWhiteSpace(idPart) || string.IsNullOrWhiteSpace(textPart))
            return null;

#if NET7_0_OR_GREATER
          if (!TryConvertToId(idPart.AsSpan(), out T id))
#else
          if (!TryConvertToId(idPart, out T id))
#endif
            return null;

          return (Id: id, Text: textPart);
        },
        s => s.Text,
        forceParallel);

      if (_search.IsPhoneticSearch)
        result = ConvertToPhonetic(result);

      AddToIndex(result);
      NotifyIndexCreated();
    }

    public void BuildIndex(IEnumerable<(string Text, T Index)> source, bool forceParallel = false)
    {
      var result = ProcessSource(
        source,
        s => (s.Index, s.Text),
        s => s.Text,
        forceParallel);

      if (_search.IsPhoneticSearch)
        result = ConvertToPhonetic(result);

      AddToIndex(result);
      NotifyIndexCreated();
    }

    private IOrderedEnumerable<(string Text, IEnumerable<T> Indexes)> ProcessSource<TInput>(
      IEnumerable<TInput> source,
      Func<TInput, (T Id, string Text)?> idAndTextSelector,
      Func<(T Id, string Text), string> textSelector,
      bool forceParallel = false)
    {
      bool useParallelProcessing = ShouldUseParallelProcessing(source, forceParallel);

      return useParallelProcessing ? ProcessSourceParallel(source, idAndTextSelector, textSelector) : ProcessSourceSequental(source, idAndTextSelector, textSelector);
    }

    private IOrderedEnumerable<(string Text, IEnumerable<T> Indexes)> ProcessSourceSequental<TInput>(
      IEnumerable<TInput> source,
      Func<TInput, (T Id, string Text)?> idAndTextSelector,
      Func<(T Id, string Text), string> textSelector)
    {
      return source
        .Select(idAndTextSelector)
        .Where(x => x.HasValue)
        .Select(x => x!.Value)
        .Select(s => (s.Id, TextList: textSelector(s).Split(_delimiters, StringSplitOptions.RemoveEmptyEntries)))
        .SelectMany(m => m.TextList, (m, v) => (Text: v.ToUpper(), m.Id))
        .Where(c => c.Text.Length > 1 && (!_hasNumberAndCharacter.IsMatch(c.Text) || _search.IsNumberSearch))
        .GroupBy(i => i.Text)
        .Select(g => (Text: g.Key, Indexes: g.Select(r => r.Id).Distinct()))
        .OrderBy(o => o.Text);
    }

    private IOrderedEnumerable<(string Text, IEnumerable<T> Indexes)> ProcessSourceParallel<TInput>(
      IEnumerable<TInput> source,
      Func<TInput, (T Id, string Text)?> idAndTextSelector,
      Func<(T Id, string Text), string> textSelector)
    {
      return source
        .AsParallel()
        .WithDegreeOfParallelism(ParallelExecutionPolicy.GetEffectiveDegreeOfParallelism(Environment.ProcessorCount))
        .Select(idAndTextSelector)
        .Where(x => x.HasValue)
        .Select(x => x!.Value)
        .Select(s => (s.Id, TextList: textSelector(s).Split(_delimiters, StringSplitOptions.RemoveEmptyEntries)))
        .SelectMany(m => m.TextList, (m, v) => (Text: v.ToUpper(), m.Id))
        .Where(c => c.Text.Length > 1 && (!_hasNumberAndCharacter.IsMatch(c.Text) || _search.IsNumberSearch))
        .AsSequential()
        .GroupBy(i => i.Text)
        .Select(g => (Text: g.Key, Indexes: g.Select(r => r.Id).Distinct()))
        .OrderBy(o => o.Text);
    }

    private bool ShouldUseParallelProcessing<TInput>(IEnumerable<TInput> sources, bool forceParallel)
    {
      int count = GetSourceCount(sources);

      return ParallelExecutionPolicy.ShouldUseParallel(
        Environment.ProcessorCount,
        forceParallel,
        count,
        _parallelProcessingThreshold);
    }

    private static int GetSourceCount<TInput>(IEnumerable<TInput> sources)
    {
      if (sources.TryGetNonEnumeratedCount(out var count))
        return count;

      return sources.Count();
    }

    private void AddToIndex(IOrderedEnumerable<(string Text, IEnumerable<T> Indexes)> result)
    {
      foreach (var (Text, Indexes) in result)
        _search._searchIndex!.Add(Text, new(Indexes));

      if (_search.IsPhoneticSearch)
        _search.RebuildPhoneticCandidateIndex();

      _search.IsIndexComplete = true;
    }

    private void NotifyIndexCreated() => IndexCreated?.Invoke(this, EventArgs.Empty);

    private static IOrderedEnumerable<(string Text, IEnumerable<T> Indexes)> ConvertToPhonetic(IOrderedEnumerable<(string Text, IEnumerable<T> Indexes)> result)
    {
      return result
        .Select(p => (Text: PhoneticSearch.MetaPhone(p.Text), p.Indexes))
        .GroupBy(x => x.Text)
        .Select(g => (Text: g.Key, Indexes: g
          .Select(t => t.Indexes)
          .Aggregate((r, c) => r.Union(c))
          .Distinct()))
        .OrderBy(o => o.Text);
    }

#if NET7_0_OR_GREATER
    private static bool TryConvertToId(ReadOnlySpan<char> source, out T value) => DefaultIdParser<T>.TryParse(source, out value);
#else
    private static bool TryConvertToId(string source, out T value) => DefaultIdParser<T>.TryParse(source, out value);
#endif
    #endregion

    #region Событие
    public event EventHandler<EventArgs>? IndexCreated;
    #endregion
  }
}