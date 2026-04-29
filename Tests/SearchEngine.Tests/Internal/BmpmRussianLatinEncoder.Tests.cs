namespace SearchEngine.Tests;

public class BmpmRussianLatinEncoderTests
{
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("Иванов")]
  [InlineData("Ivanov2")]
  public void Encode_НеподходящееИмя_ДолженВернутьПустойНабор(string? source)
  {
    // Act
    IReadOnlyList<string> result = BmpmRussianLatinEncoder.Encode(source);

    // Assert
    Assert.Empty(result);
  }

  [Theory]
  [InlineData("Ivanov", "ИФАНОФ")]
  [InlineData("Petrova", "ПЕТРОФА")]
  [InlineData("Shcherbakov", "ШЕРПАКОФ")]
  public void Encode_РусскаяЛатиница_ДолженВернутьСтрогийФонетическийКлюч(
      string source,
      string expected)
  {
    // Act
    IReadOnlyList<string> result = BmpmRussianLatinEncoder.Encode(source);

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
    IReadOnlyList<string> result = BmpmRussianLatinEncoder.EncodeApprox(source);

    // Assert
    string key = Assert.Single(result);
    Assert.Equal(expected, key);
  }
}