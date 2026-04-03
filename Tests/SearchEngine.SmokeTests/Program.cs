using System.Text;

using ResultType;

using SearchEngine;

namespace SearchEngineSmokeTests
{
  internal static class Program
  {
    private static int _passed;
    private static int _failed;

    private static async Task<int> Main()
    {
      Console.OutputEncoding = Encoding.UTF8;

      await RunScenarioAsync("Успешная индексация и поиск", ПроверитьУспешнуюИндексациюИПоискAsync);
      await RunScenarioAsync("Ошибки индексации", ПроверитьОшибкиИндексацииAsync);
      await RunScenarioAsync("Ошибки поиска", ПроверитьОшибкиПоискаAsync);

      Console.WriteLine(new string('=', 70));
      Console.WriteLine("ИТОГ");
      Console.WriteLine($"Успешно: {_passed}");
      Console.WriteLine($"Ошибок:  {_failed}");
      Console.WriteLine(new string('=', 70));

      return _failed == 0 ? 0 : 1;
    }

    /// <summary>
    /// Выполняет сценарий smoke-проверки и перехватывает неожиданные ошибки.
    /// </summary>
    /// <param name="title">Название сценария.</param>
    /// <param name="scenario">Код сценария.</param>
    private static async Task RunScenarioAsync(string title, Func<Task> scenario)
    {
      Console.WriteLine();
      Console.WriteLine(new string('=', 70));
      Console.WriteLine(title);
      Console.WriteLine(new string('=', 70));

      try
      {
        await scenario();
      }
      catch (Exception exception)
      {
        Fail(
            $"{title}: необработанное исключение",
            $"{exception.GetType().Name}: {exception.Message}");
      }
    }

    /// <summary>
    /// Проверяет успешную индексацию и корректный поиск.
    /// </summary>
    private static async Task ПроверитьУспешнуюИндексациюИПоискAsync()
    {
      Search<int> search = new()
      {
        SearchType = SearchType.ExactSearch,
        SearchLocation = SearchLocation.BeginWord
      };

      string[] source =
      [
          "1;процесс согласования договоров",
            "2;красный велосипед",
            "3;иванов сергей петрович",
            "4;товар;с дополнительным;разделителем"
      ];

      UnitResult<SearchError> prepareResult = await search.PrepareIndexResult(source, ";");

      Check(
          "Индексация корректного источника должна быть успешной",
          prepareResult.IsSuccess,
          prepareResult.IsFailure
              ? $"{prepareResult.Error!.Code}: {prepareResult.Error.Message}"
              : null);

      if (prepareResult.IsFailure)
      {
        return;
      }

      Result<SearchResultList<int>, SearchError> processSearch = search.FindResult("процесс");
      CheckSearchSuccess(
          "Поиск слова \"процесс\" должен вернуть запись с Id = 1",
          processSearch,
          [1]);

      Result<SearchResultList<int>, SearchError> surnameSearch = search.FindResult("иванов");
      CheckSearchSuccess(
          "Поиск слова \"иванов\" должен вернуть запись с Id = 3",
          surnameSearch,
          [3]);

      Result<SearchResultList<int>, SearchError> additionalSearch = search.FindResult("дополнительным");
      CheckSearchSuccess(
          "Поиск слова \"дополнительным\" должен вернуть запись с Id = 4",
          additionalSearch,
          [4]);

      Result<SearchResultList<int>, SearchError> delimiterTailSearch = search.FindResult("разделителем");
      CheckSearchSuccess(
          "Поиск слова \"разделителем\" должен вернуть запись с Id = 4",
          delimiterTailSearch,
          [4]);
    }

    /// <summary>
    /// Проверяет ожидаемые ошибки индексации.
    /// </summary>
    private static async Task ПроверитьОшибкиИндексацииAsync()
    {
      await ПроверитьОшибкуИндексацииAsync(
          "Пустой разделитель должен вернуть InvalidDelimitedSourceFormat",
          [
                "1;процесс согласования договоров"
          ],
          string.Empty,
          SearchErrorCode.InvalidDelimitedSourceFormat);

      await ПроверитьОшибкуИндексацииAsync(
          "Строка без разделителя должна вернуть InvalidDelimitedSourceFormat",
          [
                "1 процесс согласования договоров"
          ],
          ";",
          SearchErrorCode.InvalidDelimitedSourceFormat);

      await ПроверитьОшибкуИндексацииAsync(
          "Битый идентификатор должен вернуть InvalidIdFormat",
          [
                "abc;процесс согласования договоров"
          ],
          ";",
          SearchErrorCode.InvalidIdFormat);

      await ПроверитьОшибкуИндексацииAsync(
          "Пустой текст должен вернуть InvalidDelimitedSourceFormat",
          [
                "1;"
          ],
          ";",
          SearchErrorCode.InvalidDelimitedSourceFormat);
    }

    /// <summary>
    /// Проверяет ожидаемые ошибки поиска.
    /// </summary>
    private static Task ПроверитьОшибкиПоискаAsync()
    {
      Search<int> search = new();

      Result<SearchResultList<int>, SearchError> emptyQueryResult = search.FindResult(string.Empty);
      CheckFailure(
          "Пустой запрос должен вернуть EmptyQuery",
          emptyQueryResult,
          SearchErrorCode.EmptyQuery);

      Result<SearchResultList<int>, SearchError> searchWithoutIndexResult = search.FindResult("процесс");
      CheckFailure(
          "Поиск без подготовленного индекса должен вернуть IndexNotBuilt",
          searchWithoutIndexResult,
          SearchErrorCode.IndexNotBuilt);

      return Task.CompletedTask;
    }

    /// <summary>
    /// Выполняет один негативный сценарий индексации.
    /// </summary>
    /// <param name="title">Название проверки.</param>
    /// <param name="source">Источник данных.</param>
    /// <param name="delimiter">Разделитель между идентификатором и текстом.</param>
    /// <param name="expectedErrorCode">Ожидаемый код ошибки.</param>
    private static async Task ПроверитьОшибкуИндексацииAsync(
        string title,
        string[] source,
        string delimiter,
        SearchErrorCode expectedErrorCode)
    {
      Search<int> search = new();

      UnitResult<SearchError> result = await search.PrepareIndexResult(source, delimiter);

      Check(
          title,
          result.IsFailure && result.Error!.Code == expectedErrorCode,
          result.IsSuccess
              ? "Ожидалась ошибка, но операция завершилась успешно."
              : $"Получен код {result.Error!.Code}: {result.Error.Message}");
    }

    /// <summary>
    /// Проверяет успешный результат поиска и наличие ожидаемых идентификаторов.
    /// </summary>
    /// <param name="title">Название проверки.</param>
    /// <param name="result">Результат поиска.</param>
    /// <param name="expectedIds">Ожидаемые идентификаторы.</param>
    private static void CheckSearchSuccess(
        string title,
        Result<SearchResultList<int>, SearchError> result,
        params int[] expectedIds)
    {
      if (result.IsFailure)
      {
        Fail(title, $"{result.Error!.Code}: {result.Error.Message}");
        return;
      }

      if (!result.Value!.IsHasIndex)
      {
        Fail(title, "Поиск завершился успешно, но результаты отсутствуют.");
        return;
      }

      foreach (int expectedId in expectedIds)
      {
        if (!ContainsId(result.Value, expectedId))
        {
          Fail(title, $"В результатах не найден Id = {expectedId}.");
          PrintSearchBuckets(result.Value);
          return;
        }
      }

      Pass(title);
      PrintSearchBuckets(result.Value);
    }

    /// <summary>
    /// Проверяет, что результат завершился ошибкой с ожидаемым кодом.
    /// </summary>
    /// <param name="title">Название проверки.</param>
    /// <param name="result">Результат поиска.</param>
    /// <param name="expectedErrorCode">Ожидаемый код ошибки.</param>
    private static void CheckFailure(
        string title,
        Result<SearchResultList<int>, SearchError> result,
        SearchErrorCode expectedErrorCode)
    {
      Check(
          title,
          result.IsFailure && result.Error!.Code == expectedErrorCode,
          result.IsSuccess
              ? "Ожидалась ошибка, но получен успешный результат."
              : $"Получен код {result.Error!.Code}: {result.Error.Message}");
    }

    /// <summary>
    /// Проверяет, содержится ли идентификатор в результатах поиска.
    /// </summary>
    /// <param name="result">Результат поиска.</param>
    /// <param name="id">Искомый идентификатор.</param>
    /// <returns>
    /// <see langword="true"/>, если идентификатор найден; иначе <see langword="false"/>.
    /// </returns>
    private static bool ContainsId(SearchResultList<int> result, int id)
    {
      foreach (var bucket in result.Items)
      {
        string value = bucket.Value.ToString();

        string[] parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Any(x => x == id.ToString()))
        {
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Печатает найденные корзины результатов.
    /// </summary>
    /// <param name="result">Результат поиска.</param>
    private static void PrintSearchBuckets(SearchResultList<int> result)
    {
      Console.WriteLine("Результаты поиска:");

      foreach (var bucket in result.Items)
        Console.WriteLine($"  Дистанция: {bucket.Key}; Id: {bucket.Value}");
    }

    /// <summary>
    /// Универсальная проверка логического условия.
    /// </summary>
    /// <param name="title">Название проверки.</param>
    /// <param name="condition">Проверяемое условие.</param>
    /// <param name="details">Дополнительные детали при ошибке.</param>
    private static void Check(string title, bool condition, string? details = null)
    {
      if (condition)
      {
        Pass(title);
        return;
      }

      Fail(title, details);
    }

    /// <summary>
    /// Фиксирует успешное выполнение проверки.
    /// </summary>
    /// <param name="title">Название проверки.</param>
    private static void Pass(string title)
    {
      _passed++;
      Console.WriteLine($"[OK]   {title}");
    }

    /// <summary>
    /// Фиксирует неуспешное выполнение проверки.
    /// </summary>
    /// <param name="title">Название проверки.</param>
    /// <param name="details">Дополнительные детали.</param>
    private static void Fail(string title, string? details = null)
    {
      _failed++;
      Console.WriteLine($"[FAIL] {title}");

      if (!string.IsNullOrWhiteSpace(details))
        Console.WriteLine($"       {details}");
    }
  }
}