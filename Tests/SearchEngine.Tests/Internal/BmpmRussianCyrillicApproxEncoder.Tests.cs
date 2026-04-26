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
  [InlineData("Петров", "ПИТРАФ")]
  [InlineData("Смирнов", "СМИРНАФ")]
  [InlineData("Семёнов", "СИМИНАФ")]
  [InlineData("Папондопуло", "ПАПАНТАПУЛА")]
  public void Encode_РусскаяФамилия_ДолженВернутьПриближённыйКлюч(
    string source,
    string expectedKey)
  {
    // Act
    IReadOnlyList<string> result = BmpmRussianCyrillicApproxEncoder.Encode(source);

    // Assert
    string key = Assert.Single(result);
    Assert.Equal(expectedKey, key);
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