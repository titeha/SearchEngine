namespace SearchEngine.Tests;

/// <summary>
/// Тесты правил выбора режима параллельного выполнения.
/// </summary>
public class ParallelExecutionPolicyTests
{
  [Theory]
  [InlineData(0, 1)]
  [InlineData(1, 1)]
  [InlineData(2, 2)]
  [InlineData(3, 2)]
  [InlineData(4, 3)]
  [InlineData(8, 6)]
  public void GetEffectiveDegreeOfParallelism_ДолженВозвращатьБезопасноеЗначение(
    int processorCount,
    int expected)
  {
    int actual = ParallelExecutionPolicy.GetEffectiveDegreeOfParallelism(processorCount);

    Assert.Equal(expected, actual);
  }

  [Theory]
  [InlineData(0, false)]
  [InlineData(1, false)]
  [InlineData(2, true)]
  [InlineData(8, true)]
  public void CanUseParallel_ДолженУчитыватьКоличествоЛогическихПроцессоров(
     int processorCount,
     bool expected)
  {
    bool actual = ParallelExecutionPolicy.CanUseParallel(processorCount);

    Assert.Equal(expected, actual);
  }

  [Theory]
  [InlineData(1, false, 100_000, 10_000, false)]
  [InlineData(1, true, 100_000, 10_000, false)]
  [InlineData(2, false, 1_000, 10_000, false)]
  [InlineData(2, false, 20_000, 10_000, true)]
  [InlineData(2, true, 1, 10_000, true)]
  public void ShouldUseParallel_ДолженУчитыватьПроцессорыФлагИПорог(
     int processorCount,
     bool forceParallel,
     int itemCount,
     int threshold,
     bool expected)
  {
    bool actual = ParallelExecutionPolicy.ShouldUseParallel(
    processorCount,
    forceParallel,
    itemCount,
    threshold);

    Assert.Equal(expected, actual);
  }
}