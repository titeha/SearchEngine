namespace SearchEngine.Tests;

public class BmpmRussianCyrillicApproxEncoderTests
{
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("Ivanov")]
  [InlineData("Иванов2")]
  public void Encode_НеподходящееИмя_ДолженВернутьПустойНабор(string? source)
  {
    // Act
    IReadOnlyList<string> result = BmpmRussianCyrillicApproxEncoder.Encode(source);

    // Assert
    Assert.Empty(result);
  }

  [Theory]
  [InlineData("Петров", "ПЕТРОФ", "ПИТРАФ")]
  [InlineData("Смирнов", "СМИРНОФ", "СМИРНАФ")]
  [InlineData("Семёнов", "СЕМЕНОФ", "СИМИНАФ")]
  [InlineData("Папондопуло", "ПАПОНТОПУЛО", "ПАПАНТАПУЛА")]
  public void Encode_РусскаяФамилия_ДолженВернутьТочныйИПриближённыйКлюч(
      string source,
      string exactKey,
      string approxKey)
  {
    // Act
    IReadOnlyList<string> result = BmpmRussianCyrillicApproxEncoder.Encode(source);

    // Assert
    Assert.Equal(2, result.Count);
    Assert.Equal(exactKey, result[0]);
    Assert.Equal(approxKey, result[1]);
  }

  [Theory]
  [InlineData("Забабанова", "Забабонова", "САПАПАНАФА")]
  [InlineData("Папандопуло", "Папондопуло", "ПАПАНТАПУЛА")]
  public void Encode_СозвучныеФамилии_ДолженВернутьОбщийПриближённыйКлюч(
      string first,
      string second,
      string expectedKey)
  {
    // Act
    IReadOnlyList<string> firstResult = BmpmRussianCyrillicApproxEncoder.Encode(first);
    IReadOnlyList<string> secondResult = BmpmRussianCyrillicApproxEncoder.Encode(second);

    // Assert
    Assert.Contains(expectedKey, firstResult);
    Assert.Contains(expectedKey, secondResult);
  }

  [Theory]
  [InlineData("Алла", "АЛА")]
  [InlineData("Анна", "АНА")]
  public void Encode_ПриближённыйКлючСовпадаетСТочным_ДолженВернутьОдинКлюч(
      string source,
      string expectedKey)
  {
    // Act
    IReadOnlyList<string> result = BmpmRussianCyrillicApproxEncoder.Encode(source);

    // Assert
    string key = Assert.Single(result);
    Assert.Equal(expectedKey, key);
  }
}