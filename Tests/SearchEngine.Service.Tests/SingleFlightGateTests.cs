using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты lock-free шлюза «один в полёте».
/// </summary>
public sealed class SingleFlightGateTests
{
  /// <summary>
  /// Проверяет, что свободный шлюз можно занять и он становится занятым.
  /// </summary>
  [Fact]
  public void TryEnter_СвободныйШлюз_ЗанимаетШлюз()
  {
    // Arrange
    SingleFlightGate sut = new();

    // Act
    bool entered = sut.TryEnter();

    // Assert
    Assert.True(entered);
    Assert.True(sut.IsInProgress);
  }

  /// <summary>
  /// Проверяет, что повторный вход в занятый шлюз не выполняется.
  /// </summary>
  [Fact]
  public void TryEnter_ЗанятыйШлюз_НеЗанимаетПовторно()
  {
    // Arrange
    SingleFlightGate sut = new();
    sut.TryEnter();

    // Act
    bool secondEntered = sut.TryEnter();

    // Assert
    Assert.False(secondEntered);
    Assert.True(sut.IsInProgress);
  }

  /// <summary>
  /// Проверяет, что после освобождения шлюз снова можно занять.
  /// </summary>
  [Fact]
  public void Exit_ПослеОсвобождения_ПозволяетЗанятьСнова()
  {
    // Arrange
    SingleFlightGate sut = new();
    sut.TryEnter();

    // Act
    sut.Exit();
    bool reentered = sut.TryEnter();

    // Assert
    Assert.True(reentered);
    Assert.True(sut.IsInProgress);
  }

  /// <summary>
  /// Проверяет, что при конкурентной гонке шлюз занимает ровно один вызывающий.
  /// </summary>
  [Fact]
  public void TryEnter_ПриКонкурентнойГонке_ЗанимаетТолькоОдин()
  {
    // Arrange
    SingleFlightGate sut = new();
    const int workerCount = 64;
    int enteredCount = 0;

    // Act
    Parallel.For(0, workerCount, _ =>
    {
      if (sut.TryEnter())
        Interlocked.Increment(ref enteredCount);
    });

    // Assert
    Assert.Equal(1, enteredCount);
  }
}
