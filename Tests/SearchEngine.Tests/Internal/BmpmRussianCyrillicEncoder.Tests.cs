namespace SearchEngine.Tests;

public class BmpmRussianCyrillicEncoderTests
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
    IReadOnlyList<string> result = BmpmRussianCyrillicEncoder.Encode(source);

    // Assert
    Assert.Empty(result);
  }

  [Theory]
  [InlineData("Иванов", "ИФАНОФ")]
  [InlineData("Петров", "ПЕТРОФ")]
  [InlineData("Семёнов", "СЕМЕНОФ")]
  [InlineData("Соловьёв", "СОЛОФЕФ")]
  public void Encode_РусскаяФамилия_ДолженВернутьФонетическийКлюч(
      string source,
      string expected)
  {
    // Act
    IReadOnlyList<string> result = BmpmRussianCyrillicEncoder.Encode(source);

    // Assert
    string key = Assert.Single(result);
    Assert.Equal(expected, key);
  }

  [Theory]
  [InlineData("Иванов", "Иваноф")]
  [InlineData("Семёнов", "Семенов")]
  [InlineData("Гончаров", "Кончаров")]
  [InlineData("Захаров", "Сахаров")]
  [InlineData("Щербаков", "Шербаков")]
  [InlineData("Чистяков", "Шистяков")]
  public void Encode_ФонетическиБлизкиеФамилии_ДолженВернутьОдинаковыйКлюч(
      string first,
      string second)
  {
    // Act
    IReadOnlyList<string> firstResult = BmpmRussianCyrillicEncoder.Encode(first);
    IReadOnlyList<string> secondResult = BmpmRussianCyrillicEncoder.Encode(second);

    // Assert
    string firstKey = Assert.Single(firstResult);
    string secondKey = Assert.Single(secondResult);

    Assert.Equal(firstKey, secondKey);
  }

  [Theory]
  [InlineData("Анна", "АНА")]
  [InlineData("Алла", "АЛА")]
  [InlineData("Римма", "РИМА")]
  public void Encode_СоседниеПовторы_ДолженСжатьПовторяющиесяСимволы(
      string source,
      string expected)
  {
    // Act
    IReadOnlyList<string> result = BmpmRussianCyrillicEncoder.Encode(source);

    // Assert
    string key = Assert.Single(result);
    Assert.Equal(expected, key);
  }

  [Theory]
  [InlineData("Салтыков-Щедрин", "САЛТИКОФШЕТРИН")]
  [InlineData(" Дьяченко ", "ТАШЕНКО")]
  public void Encode_НормализуемоеИмя_ДолженСначалаНормализоватьСтроку(
      string source,
      string expected)
  {
    // Act
    IReadOnlyList<string> result = BmpmRussianCyrillicEncoder.Encode(source);

    // Assert
    string key = Assert.Single(result);
    Assert.Equal(expected, key);
  }
}