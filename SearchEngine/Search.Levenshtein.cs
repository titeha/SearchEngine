using CommonClasses;

using static System.Math;

namespace SearchEngine;

public partial class Search<T> where T : struct
{
  internal static class Levenshtein
  {
    public static int DistanceLeventstein(string source, string target)
    {
      if (source.IsNullOrEmpty())
      {
        if (target.IsNullOrEmpty())
          return 0;
        return target.Length * 2;
      }
      if (target.IsNullOrEmpty())
        return source.Length * 2;

      int targetLength = target.Length;
      int sourceLength = source.Length;
      int[,] distance = new int[3, targetLength + 1];

      for (int j = 1; j < targetLength; j++)
        distance[0, j] = j * 2;

      int currentRow = 0;

      for (int i = 1; i <= sourceLength; i++)
      {
        currentRow = i % 3;
        int previousRow = (i - 1) % 3;
        distance[currentRow, 0] = i * 2;
        int compensation = i == sourceLength ? 1 : 2;

        for (int j = 1; j <= targetLength; j++)
        {
          distance[currentRow, j] = Min(
            Min(
              distance[previousRow, j] + compensation,
              distance[currentRow, j - 1] + compensation),
            distance[previousRow, j - 1] + CostDistanceSymbol(source[i - 1], target[j - 1]));
          if (i > 1
              && j > 1
              && _keyCodesRUEN[source[i - 1]] == _keyCodesRUEN[target[j - 2]]
              && _keyCodesRUEN[source[i - 2]] == _keyCodesRUEN[target[j - 1]])
            distance[currentRow, j] = Min(distance[currentRow, j], distance[(i - 2) % 3, j - 2] + 2);
        }
      }

      return distance[currentRow, targetLength];
    }

    private static int CostDistanceSymbol(char source, char target)
    {
      if (source == target)
        return 0;

      int sourceComparerCode = _keyCodesRUEN[source];
      int targetComparerCode = _keyCodesRUEN[target];
      if (sourceComparerCode != 0 && sourceComparerCode == targetComparerCode)
        return 0;

      if (_distanceCodeKey.TryGetValue(sourceComparerCode, out var nearKeys))
        return nearKeys.Contains(targetComparerCode) ? 1 : 2;
      else
        return 2;
    }
  }
}