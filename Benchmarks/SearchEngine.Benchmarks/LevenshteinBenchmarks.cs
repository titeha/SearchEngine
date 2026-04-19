using BenchmarkDotNet.Attributes;

namespace SearchEngine.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class LevenshteinBenchmarks
{
  private string _source = string.Empty;
  private string _target = string.Empty;

  [Params(4, 8, 16, 32, 48)]
  public int Length { get; set; }

  [Params(
    LevenshteinScenario.Equal,
    LevenshteinScenario.ReplaceOne,
    LevenshteinScenario.InsertOne,
    LevenshteinScenario.DeleteOne,
    LevenshteinScenario.TransposeNearEnd,
    LevenshteinScenario.Different)]
  public LevenshteinScenario Scenario { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    _source = CreateWord(Length);

    _target = Scenario switch
    {
      LevenshteinScenario.Equal => _source,
      LevenshteinScenario.ReplaceOne => ReplaceOne(_source),
      LevenshteinScenario.InsertOne => InsertOne(_source),
      LevenshteinScenario.DeleteOne => DeleteOne(_source),
      LevenshteinScenario.TransposeNearEnd => TransposeNearEnd(_source),
      LevenshteinScenario.Different => CreateDifferentWord(Length),
      _ => throw new InvalidOperationException($"Неизвестный сценарий {Scenario}.")
    };

    // В реальном поисковом пути токены нормализуются до верхнего регистра.
    // Микробенчмарк вызывает внутренний алгоритм напрямую, поэтому обязан
    // выполнить такую же нормализацию сам.
    _source = _source.ToUpperInvariant();
    _target = _target.ToUpperInvariant();

    // Sanity-check до старта BenchmarkDotNet-итераций.
    // Если внутренний алгоритм снова упадёт на тестовых данных,
    // мы увидим проблему сразу в GlobalSetup.
    _ = Search<int>.Levenshtein.DistanceLevenshtein(_source, _target);
  }

  [Benchmark]
  public int Distance()
  {
    return Search<int>.Levenshtein.DistanceLevenshtein(_source, _target);
  }

  private static string CreateWord(int length)
  {
    const string alphabet = "абвгдежзиклмнопрстуфхцчшэюя";

    char[] result = new char[length];

    for (int i = 0; i < result.Length; i++)
      result[i] = alphabet[(i * 7 + length) % alphabet.Length];

    return new string(result);
  }

  private static string CreateDifferentWord(int length)
  {
    const string alphabet = "яюэшчцхфутсрпонмлкизжедгвба";

    char[] result = new char[length];

    for (int i = 0; i < result.Length; i++)
      result[i] = alphabet[(i * 11 + length) % alphabet.Length];

    return new string(result);
  }

  private static string ReplaceOne(string value)
  {
    char[] result = value.ToCharArray();

    int position = result.Length / 2;
    result[position] = result[position] == 'о' ? 'а' : 'о';

    return new string(result);
  }

  private static string InsertOne(string value)
  {
    int position = value.Length / 2;

    return value.Insert(position, "а");
  }

  private static string DeleteOne(string value)
  {
    if (value.Length <= 1)
      return string.Empty;

    int position = value.Length / 2;

    return value.Remove(position, 1);
  }

  private static string TransposeNearEnd(string value)
  {
    if (value.Length < 2)
      return value;

    char[] result = value.ToCharArray();

    int first = result.Length - 2;
    int second = result.Length - 1;

    (result[first], result[second]) = (result[second], result[first]);

    return new string(result);
  }
}

public enum LevenshteinScenario
{
  Equal,
  ReplaceOne,
  InsertOne,
  DeleteOne,
  TransposeNearEnd,
  Different
}