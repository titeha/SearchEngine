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
  [InlineData("Ivanov", "ИФАНОФ")]
  [InlineData("Petrova", "ПЕТРОФА")]
  [InlineData("Shcherbakov", "ШЕРПАКОФ")]
  public void Encode_РусскаяЛатиница_ДолженВернутьФонетическийКлюч(
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
  [InlineData("Ivanov", "ИФАНАФ")]
  [InlineData("Petrova", "ПИТРАФА")]
  [InlineData("Shcherbakov", "ШИРПАКАФ")]
  public void EncodeApprox_РусскаяЛатиница_ДолженВернутьПриближённыйФонетическийКлюч(
    string source,
    string expected)
  {
    // Act
    IReadOnlyList<string> result = BmpmPhoneticEncoder.EncodeApprox(source);

    // Assert
    string key = Assert.Single(result);
    Assert.Equal(expected, key);
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
  [InlineData("Smith")]
  [InlineData("Johnson")]
  [InlineData("Иванов Smith")]
  [InlineData("12345")]
  public void EncodeApprox_НеподдерживаемоеИмя_ДолженВернутьПустойНабор(string source)
  {
    // Act
    IReadOnlyList<string> result = BmpmPhoneticEncoder.EncodeApprox(source);

    // Assert
    Assert.Empty(result);
  }
}