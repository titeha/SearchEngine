namespace SearchEngine.Tests;

public class BmpmRussianCyrillicNormalizerTests
{
  [Theory]
  [InlineData(null, "")]
  [InlineData("", "")]
  [InlineData("   ", "")]
  public void Normalize_ПустаяСтрока_ДолженВернутьПустуюСтроку(string? source, string expected)
  {
    // Act
    string result = BmpmRussianCyrillicNormalizer.Normalize(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("иванов", "ИВАНОВ")]
  [InlineData("Петров", "ПЕТРОВ")]
  [InlineData("сИдОрОв", "СИДОРОВ")]
  public void Normalize_РусскаяКириллица_ДолженВернутьВерхнийРегистр(
      string source,
      string expected)
  {
    // Act
    string result = BmpmRussianCyrillicNormalizer.Normalize(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("Семёнов", "СЕМЕНОВ")]
  [InlineData("Соловьёв", "СОЛОВЕВ")]
  public void Normalize_БукваЁ_ДолженЗаменитьНаЕ(
      string source,
      string expected)
  {
    // Act
    string result = BmpmRussianCyrillicNormalizer.Normalize(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("Объезднов", "ОБЕЗДНОВ")]
  [InlineData("Соловьёв", "СОЛОВЕВ")]
  public void Normalize_МягкийИТвёрдыйЗнак_ДолженУдалить(
      string source,
      string expected)
  {
    // Act
    string result = BmpmRussianCyrillicNormalizer.Normalize(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("Салтыков-Щедрин", "САЛТЫКОВЩЕДРИН")]
  [InlineData("Салтыков–Щедрин", "САЛТЫКОВЩЕДРИН")]
  [InlineData(" Дьяченко ", "ДЯЧЕНКО")]
  public void Normalize_Разделители_ДолженУдалить(
      string source,
      string expected)
  {
    // Act
    string result = BmpmRussianCyrillicNormalizer.Normalize(source);

    // Assert
    Assert.Equal(expected, result);
  }

  [Theory]
  [InlineData("Ivanov")]
  [InlineData("Иванов2")]
  [InlineData("Иванов.")]
  public void Normalize_НеподдерживаемыеСимволы_ДолженВернутьПустуюСтроку(string source)
  {
    // Act
    string result = BmpmRussianCyrillicNormalizer.Normalize(source);

    // Assert
    Assert.Equal(string.Empty, result);
  }
}