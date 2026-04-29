namespace SearchEngine.Tests;

public class BmpmRussianLatinTransliteratorTests
{
  [Theory]
  [InlineData(null, "")]
  [InlineData("", "")]
  [InlineData("   ", "")]
  public void Transliterate_ПустаяСтрока_ДолженВернутьПустуюСтроку(
      string? source,
      string expected)
  {
    // Act
    string result = BmpmRussianLatinTransliterator.Transliterate(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("Ivanov", "ИВАНОВ")]
  [InlineData("Petrova", "ПЕТРОВА")]
  [InlineData("Semenov", "СЕМЕНОВ")]
  [InlineData("Smirnov", "СМИРНОВ")]
  public void Transliterate_ПростаяРусскаяЛатиница_ДолженВернутьКириллицу(
      string source,
      string expected)
  {
    // Act
    string result = BmpmRussianLatinTransliterator.Transliterate(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("Shcherbakov", "ЩЕРБАКОВ")]
  [InlineData("Schukin", "ЩУКИН")]
  [InlineData("Zhukov", "ЖУКОВ")]
  [InlineData("Kharlamov", "ХАРЛАМОВ")]
  [InlineData("Tchaikovsky", "ЧАИКОВСКИ")]
  [InlineData("Dzhafarov", "ДЖАФАРОВ")]
  public void Transliterate_СоставныеСочетания_ДолженВернутьКириллицу(
      string source,
      string expected)
  {
    // Act
    string result = BmpmRussianLatinTransliterator.Transliterate(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("Ivanov-Smirnov", "ИВАНОВСМИРНОВ")]
  [InlineData("Petrov Sidorov", "ПЕТРОВСИДОРОВ")]
  [InlineData("D'Yachenko", "ДЯЧЕНКО")]
  public void Transliterate_Разделители_ДолженУдалить(
      string source,
      string expected)
  {
    // Act
    string result = BmpmRussianLatinTransliterator.Transliterate(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("Иванов")]
  [InlineData("Ivanov2")]
  [InlineData("Ivanov.")]
  public void Transliterate_НеподдерживаемыеСимволы_ДолженВернутьПустуюСтроку(string source)
  {
    // Act
    string result = BmpmRussianLatinTransliterator.Transliterate(source);

    // Assert
    Assert.Equal(string.Empty, result);
  }
}