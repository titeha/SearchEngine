using System.Globalization;

using StringFunctions;

namespace SearchEngine;

/// <summary>
/// Внутренний parser для идентификаторов сущностей, используемых в индексе поиска.
/// Поддерживает только практические типы PK: целочисленные типы и <see cref="Guid"/>.
/// </summary>
/// <typeparam name="T">Тип идентификатора.</typeparam>
internal static class DefaultIdParser<T> where T : struct
{
  public static bool TryParse(string? source, out T value)
  {
    if (source.IsNullOrWhiteSpace())
    {
      value = default;
      return false;
    }

#if NET7_0_OR_GREATER
    return TryParse(source.AsSpan(), out value);
#else
    return TryParseCore(source!, out value);
#endif
  }

#if NET7_0_OR_GREATER
  public static bool TryParse(ReadOnlySpan<char> source, out T value)
  {
    if (source.Length == 0)
    {
      value = default;
      return false;
    }

    if (typeof(T) == typeof(byte))
    {
      bool ok = byte.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte parsed);
      value = ok ? (T)(object)parsed : default;
      return ok;
    }

    if (typeof(T) == typeof(short))
    {
      bool ok = short.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out short parsed);
      value = ok ? (T)(object)parsed : default;
      return ok;
    }

    if (typeof(T) == typeof(ushort))
    {
      bool ok = ushort.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort parsed);
      value = ok ? (T)(object)parsed : default;
      return ok;
    }

    if (typeof(T) == typeof(int))
    {
      bool ok = int.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed);
      value = ok ? (T)(object)parsed : default;
      return ok;
    }

    if (typeof(T) == typeof(uint))
    {
      bool ok = uint.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsed);
      value = ok ? (T)(object)parsed : default;
      return ok;
    }

    if (typeof(T) == typeof(long))
    {
      bool ok = long.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed);
      value = ok ? (T)(object)parsed : default;
      return ok;
    }

    if (typeof(T) == typeof(ulong))
    {
      bool ok = ulong.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed);
      value = ok ? (T)(object)parsed : default;
      return ok;
    }

    if (typeof(T) == typeof(Guid))
    {
      bool ok = Guid.TryParse(source, out Guid parsed);
      value = ok ? (T)(object)parsed : default;
      return ok;
    }

    value = default;
    return false;
  }
#else
  private static bool TryParseCore(string source, out T value)
  {
      if (typeof(T) == typeof(byte))
      {
          bool ok = byte.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte parsed);
          value = ok ? (T)(object)parsed : default;
          return ok;
      }

      if (typeof(T) == typeof(short))
      {
          bool ok = short.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out short parsed);
          value = ok ? (T)(object)parsed : default;
          return ok;
      }

      if (typeof(T) == typeof(ushort))
      {
          bool ok = ushort.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort parsed);
          value = ok ? (T)(object)parsed : default;
          return ok;
      }

      if (typeof(T) == typeof(int))
      {
          bool ok = int.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed);
          value = ok ? (T)(object)parsed : default;
          return ok;
      }

      if (typeof(T) == typeof(uint))
      {
          bool ok = uint.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsed);
          value = ok ? (T)(object)parsed : default;
          return ok;
      }

      if (typeof(T) == typeof(long))
      {
          bool ok = long.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed);
          value = ok ? (T)(object)parsed : default;
          return ok;
      }

      if (typeof(T) == typeof(ulong))
      {
          bool ok = ulong.TryParse(source, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed);
          value = ok ? (T)(object)parsed : default;
          return ok;
      }

      if (typeof(T) == typeof(Guid))
      {
          bool ok = Guid.TryParse(source, out Guid parsed);
          value = ok ? (T)(object)parsed : default;
          return ok;
      }

      value = default;
      return false;
  }
#endif
}
