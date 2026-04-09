using System.Reflection;

namespace SearchEngine.Tests;

public sealed class LegacyApiDeprecationTests
{
  [Fact]
  public void Find_ShouldBeMarkedObsolete()
  {
    MethodInfo? method = typeof(Search<int>).GetMethod(nameof(Search<>.Find), [typeof(string)]);

    AssertObsolete(
      method,
      [nameof(Search<>.FindResult),
      nameof(Search<>.TryFind)]);
  }

  [Fact]
  public void PrepareIndex_FromSource_ShouldBeMarkedObsolete()
  {
    MethodInfo? method = typeof(SearchExtension)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .SingleOrDefault(static method =>
      {
        if (method.Name != nameof(SearchExtension.PrepareIndex))
          return false;

        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length != 3)
          return false;

        if (!parameters[0].ParameterType.IsGenericType ||
            parameters[0].ParameterType.GetGenericTypeDefinition() != typeof(Search<>))
          return false;

        Type sourceType = parameters[1].ParameterType;
        if (!sourceType.IsGenericType ||
            sourceType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
          return false;

        Type itemType = sourceType.GetGenericArguments()[0];
        return itemType.IsGenericType &&
               itemType.GetGenericTypeDefinition() == typeof(ISourceData<>);
      });

    AssertObsolete(
      method,
      [nameof(SearchExtension.PrepareIndexResult),
      nameof(SearchExtension.TryPrepareIndex)]);
  }

  [Fact]
  public void PrepareIndex_FromDelimitedSource_ShouldBeMarkedObsolete()
  {
    MethodInfo? method = typeof(SearchExtension)
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .SingleOrDefault(static method =>
      {
        if (method.Name != nameof(SearchExtension.PrepareIndex))
          return false;

        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Length == 4 &&
               parameters[0].ParameterType.IsGenericType &&
               parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(Search<>) &&
               parameters[1].ParameterType == typeof(string[]) &&
               parameters[2].ParameterType == typeof(string);
      });

    AssertObsolete(
      method,
      [nameof(SearchExtension.PrepareIndexResult),
      nameof(SearchExtension.TryPrepareIndex)]);
  }

  private static void AssertObsolete(MethodInfo? method, params string[] migrationTargets)
  {
    Assert.NotNull(method);

    ObsoleteAttribute? attribute = method!.GetCustomAttribute<ObsoleteAttribute>();

    Assert.NotNull(attribute);
    Assert.False(attribute.IsError);
    Assert.Contains("будет удалён", attribute.Message);

    foreach (string migrationTarget in migrationTargets)
      Assert.Contains(migrationTarget, attribute.Message);
  }
}
