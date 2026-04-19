using StringFunctions;

using static System.Math;

namespace SearchEngine;

public partial class Search<T> where T : struct
{
  internal static class Levenshtein
  {
    private const int _maxStackAllocatedRowLength = 128;

    public static int DistanceLevenshtein(string source, string target)
    {
      if (ReferenceEquals(source, target))
        return 0;

      if (source.IsNullOrEmpty())
        return target.IsNullOrEmpty() ? 0 : target.Length * 2;

      if (target.IsNullOrEmpty())
        return source.Length * 2;

      return DistanceLevenshtein(source.AsSpan(), target.AsSpan(), int.MaxValue);
    }

    public static int DistanceLevenshtein(string source, string target, int maxDistance)
    {
      if (maxDistance < 0)
        throw new ArgumentOutOfRangeException(nameof(maxDistance), "Максимальное расстояние не может быть отрицательным.");

      if (ReferenceEquals(source, target))
        return 0;

      if (source.IsNullOrEmpty())
        return LimitByThreshold(target.IsNullOrEmpty() ? 0 : target.Length * 2, maxDistance);

      if (target.IsNullOrEmpty())
        return LimitByThreshold(source.Length * 2, maxDistance);

      return DistanceLevenshtein(source.AsSpan(), target.AsSpan(), maxDistance);
    }

    public static int DistanceLevenshtein(ReadOnlySpan<char> source, ReadOnlySpan<char> target, int maxDistance)
    {
      if (maxDistance < 0)
        throw new ArgumentOutOfRangeException(nameof(maxDistance), "Максимальное расстояние не может быть отрицательным.");

      if (source.SequenceEqual(target))
        return 0;

      if (source.IsEmpty)
        return LimitByThreshold(target.Length * 2, maxDistance);

      if (target.IsEmpty)
        return LimitByThreshold(source.Length * 2, maxDistance);

      // Минимально возможная разница не может быть меньше разницы длин.
      // Это дешёвый отсев для ограниченного поиска.
      if (Abs(source.Length - target.Length) > maxDistance)
        return AboveThreshold(maxDistance);

      int targetLength = target.Length;
      int sourceLength = source.Length;
      int rowLength = targetLength + 1;

      Span<int> row0 = rowLength <= _maxStackAllocatedRowLength ? stackalloc int[rowLength] : new int[rowLength];
      Span<int> row1 = rowLength <= _maxStackAllocatedRowLength ? stackalloc int[rowLength] : new int[rowLength];
      Span<int> row2 = rowLength <= _maxStackAllocatedRowLength ? stackalloc int[rowLength] : new int[rowLength];

      Span<int> previousPrevious = row0;
      Span<int> previous = row1;
      Span<int> current = row2;

      for (int j = 0; j <= targetLength; j++)
        previous[j] = j * 2;

      int result = 0;

      for (int i = 1; i <= sourceLength; i++)
      {
        current[0] = i * 2;

        int minDistanceInRow = current[0];
        int compensation = i == sourceLength ? 1 : 2;

        for (int j = 1; j <= targetLength; j++)
        {
          int deleteDistance = previous[j] + compensation;
          int insertDistance = current[j - 1] + compensation;
          int replaceDistance = previous[j - 1] + CostDistanceSymbol(source[i - 1], target[j - 1]);

          int value = Min(Min(deleteDistance, insertDistance), replaceDistance);

          if (i > 1
              && j > 1
              && IsTransposition(source[i - 1], source[i - 2], target[j - 1], target[j - 2]))
            value = Min(value, previousPrevious[j - 2] + 2);

          current[j] = value;

          if (value < minDistanceInRow)
            minDistanceInRow = value;
        }

        if (minDistanceInRow > maxDistance)
          return AboveThreshold(maxDistance);

        result = current[targetLength];

        Span<int> temp = previousPrevious;
        previousPrevious = previous;
        previous = current;
        current = temp;
      }

      return LimitByThreshold(result, maxDistance);
    }

    private static int CostDistanceSymbol(char source, char target)
    {
      if (source == target)
        return 0;

      if (!TryGetKeyCode(source, out int sourceComparerCode)
          || !TryGetKeyCode(target, out int targetComparerCode))
        return 2;

      if (sourceComparerCode != 0 && sourceComparerCode == targetComparerCode)
        return 0;

      return _distanceCodeKey.TryGetValue(sourceComparerCode, out var nearKeys) && nearKeys.Contains(targetComparerCode) ? 1 : 2;
    }

    private static bool IsTransposition(char currentSource, char previousSource, char currentTarget, char previousTarget)
    {
      return TryGetKeyCode(currentSource, out int currentSourceCode)
             && TryGetKeyCode(previousSource, out int previousSourceCode)
             && TryGetKeyCode(currentTarget, out int currentTargetCode)
             && TryGetKeyCode(previousTarget, out int previousTargetCode)
             && currentSourceCode == previousTargetCode
             && previousSourceCode == currentTargetCode;
    }

    private static bool TryGetKeyCode(char symbol, out int code) => _keyCodesRUEN.TryGetValue(symbol, out code);

    private static int LimitByThreshold(int distance, int maxDistance) => distance <= maxDistance ? distance : AboveThreshold(maxDistance);

    private static int AboveThreshold(int maxDistance) => maxDistance == int.MaxValue ? int.MaxValue : maxDistance + 1;
  }
}
