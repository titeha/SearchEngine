namespace SearchEngine.Tests;

public class BmpmPhoneticEncoderTests
{
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Encode_ПустоеИмя_ДолженВернутьПустойНабор(string? source)
  {
    // Act
    IReadOnlyList<string> result = BmpmPhoneticEncoder.Encode(source);

    // Assert
    Assert.Empty(result);
  }

  [Theory]
  [InlineData("Иванов", "ИФАНОФ")]
  [InlineData("Петров", "ПЕТРОФ")]
  [InlineData("Семёнов", "СЕМЕНОФ")]
  [InlineData("Салтыков-Щедрин", "САЛТИКОФШЕТРИН")]
  public void Encode_РусскаяКириллица_ДолженВернутьФонетическийКлюч(
      string source,
      string expected)
  {
    // Act
    IReadOnlyList<string> result = BmpmPhoneticEncoder.Encode(source);

    // Assert
    string key = Assert.Single(result);
    Assert.Equal(expected, key);
  }

  [Theory]
  [InlineData("Ivanov")]
  [InlineData("Petrova")]
  [InlineData("Shcherbakov")]
  public void Encode_РусскаяЛатиница_ПокаДолженВернутьПустойНабор(string source)
  {
    // Act
    IReadOnlyList<string> result = BmpmPhoneticEncoder.Encode(source);

    // Assert
    Assert.Empty(result);
  }

  [Theory]
  [InlineData("Smith")]
  [InlineData("Иванов Smith")]
  [InlineData("Иванов2")]
  [InlineData("12345")]
  public void Encode_НеподдерживаемоеИмя_ДолженВернутьПустойНабор(string source)
  {
    // Act
    IReadOnlyList<string> result = BmpmPhoneticEncoder.Encode(source);

    // Assert
    Assert.Empty(result);
  }

  [Theory]
  [InlineData("Петров", "ПИТРАФ")]
  [InlineData("Смирнов", "СМИРНАФ")]
  [InlineData("Папондопуло", "ПАПАНТАПУЛА")]
  public void EncodeApprox_РусскаяКириллица_ДолженВернутьПриближённыйКлюч(
    string source,
    string expectedKey)
  {
    // Act
    IReadOnlyList<string> result = BmpmPhoneticEncoder.EncodeApprox(source);

    // Assert
    string key = Assert.Single(result);
    Assert.Equal(expectedKey, key);
  }

  [Theory]
  [InlineData("Ivanov")]
  [InlineData("Petrova")]
  [InlineData("Shcherbakov")]
  [InlineData("Smith")]
  public void EncodeApprox_НеподдерживаемоеИмя_ДолженВернутьПустойНабор(string source)
  {
    // Act
    IReadOnlyList<string> result = BmpmPhoneticEncoder.EncodeApprox(source);

    // Assert
    Assert.Empty(result);
  }
}