using Xunit;

namespace SearchEngine.Tests;

public class BmpmNameLanguageDetectorTests
{
  [Theory]
  [InlineData("Иванов")]
  [InlineData("Щербакова")]
  [InlineData("Сидоров-Петров")]
  public void Detect_РусскаяКириллица_ДолженОпределитьРусскуюКириллицу(string source)
  {
    // Act
    BmpmNameLanguage result = BmpmNameLanguageDetector.Detect(source);

    // Assert
    Assert.Equal(BmpmNameLanguage.RussianCyrillic, result);
  }

  [Theory]
  [InlineData("Ivanov")]
  [InlineData("Petrova")]
  [InlineData("Shcherbakov")]
  [InlineData("Tchaikovsky")]
  public void Detect_РусскаяЛатиница_ДолженОпределитьРусскуюЛатиницу(string source)
  {
    // Act
    BmpmNameLanguage result = BmpmNameLanguageDetector.Detect(source);

    // Assert
    Assert.Equal(BmpmNameLanguage.RussianLatin, result);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("Smith")]
  [InlineData("Иванов Smith")]
  [InlineData("12345")]
  public void Detect_НеопределённоеИмя_ДолженВернутьUnknown(string source)
  {
    // Act
    BmpmNameLanguage result = BmpmNameLanguageDetector.Detect(source);

    // Assert
    Assert.Equal(BmpmNameLanguage.Unknown, result);
  }
}