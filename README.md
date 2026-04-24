# Ti-Soft.SearchEngine

Библиотека для организации нечёткого и фонетического поиска по строкам БД на русском языке.

Она предназначена не для полнотекстового поиска по произвольным документам, а для поиска по строковым полям:
- названия товаров;
- описания процессов;
- фамилии, имена и другие ФИО;
- артикулы, коды и смешанные текстовые поля;
- любые другие строковые значения, которые уже лежат в БД.

Основная цель библиотеки — находить нужные записи даже тогда, когда пользователь:
- допускает опечатки;
- нажимает соседние клавиши;
- пишет «как слышит»;
- не знает точного написания фамилии или слова.

## Поддерживаемые платформы

Библиотека таргетирует:
- `net6.0`
- `net7.0`
- `net8.0`
- `net9.0`
- `net10.0`

## Установка

После публикации пакета:

```bash
dotnet add package Ti-Soft.SearchEngine
```

## Быстрый старт

### Подготовка данных

```csharp
using SearchEngine;

public sealed record Product : ISourceData<int>
{
    public int Id { get; init; }
    public required string Text { get; init; }
}
```

### Подготовка индекса и поиск

```csharp
using SearchEngine;

List<Product> products =
[
    new() { Id = 1, Text = "Красный велосипед" },
    new() { Id = 2, Text = "Согласование договора" },
    new() { Id = 3, Text = "Иванов Сергей Петрович" }
];

Search<int> search = new();

var prepareResult = await search.PrepareIndexResult(products);
if (prepareResult.IsFailure)
{
    Console.WriteLine($"{prepareResult.Error.Code}: {prepareResult.Error.Message}");
    return;
}

var result = search.FindResult(
    "согласование договра",
    new SearchRequest
    {
        MatchMode = QueryMatchMode.SoftAllTerms,
        SearchType = SearchType.NearSearch,
        SearchLocation = SearchLocation.BeginWord,
        AcceptableCountMisprint = 1
    });

if (result.IsFailure)
{
    Console.WriteLine($"{result.Error.Code}: {result.Error.Message}");
    return;
}

foreach (var bucket in result.Value.Items)
{
    Console.WriteLine($"Ранг/дистанция: {bucket.Key}");

    foreach (var id in bucket.Value.Items)
    {
        Console.WriteLine($"  Id: {id}");
    }
}
```

## API библиотеки

Для подготовки индекса и выполнения поиска используйте:
- `PrepareIndexResult(...)`
- `FindResult(...)`

Оба метода возвращают типизированный результат выполнения и не выбрасывают исключения
в ожидаемых прикладных сценариях.

`PrepareIndexResult(...)` подготавливает поисковый индекс и возвращает ошибку,
если входные данные некорректны или индекс не удалось построить.

`FindResult(...)` выполняет поиск по подготовленному индексу и возвращает либо результат поиска,
либо описание ошибки выполнения.

## Ошибки

Для API библиотеки используются:
- `SearchError`
- `SearchErrorCode`

Это позволяет различать:
- ошибки входных данных;
- ошибки индексации;
- ошибки выполнения поиска;
- некорректные параметры запроса.

## Режимы поиска

### `AllTerms`
Строгий режим.

Все слова запроса должны найтись в записи.
Это поведение удобно для большинства бизнес-сценариев, когда пользователь вводит несколько ключевых слов
и ожидает именно пересечение по ним.

### `AnyTerm`
Мягкое объединение по словам.

Достаточно совпадения по любому слову запроса.
Подходит для широкого поиска и автодополнения.

### `SoftAllTerms`
Гибридный режим.

Сначала выше ранжируются записи, где нашлось больше слов запроса.
Внутри одной группы результаты дополнительно ранжируются по суммарной дистанции.
Если полных совпадений нет, возвращаются частичные.

## Типы поиска

### `SearchType.ExactSearch`
Точный поиск без допуска опечаток.

### `SearchType.NearSearch`
Неточный поиск с учётом опечаток и близких клавиш.

## Место поиска

### `SearchLocation.BeginWord`
Поиск только с начала слова.

### `SearchLocation.InWord`
Поиск внутри слова, включая начало.

## Фонетический поиск

Для поиска фамилий и похожих имён можно использовать фонетический режим.

```csharp
using SearchEngine;

public sealed record Person : ISourceData<int>
{
    public int Id { get; init; }
    public required string Text { get; init; }
}

Search<int> search = new(isPhoneticSearch: true);

var prepareResult = await search.PrepareIndexResult(
[
    new Person { Id = 1, Text = "Иванов" },
    new Person { Id = 2, Text = "Петров" },
    new Person { Id = 3, Text = "Терентьев" }
]);

var result = search.FindResult("тирентев");
```

Пример выше полезен в сценариях, когда пользователь пишет фамилию «на слух».

## Когда использовать библиотеку

Библиотека особенно полезна, если поиск нужен по строкам БД, а не по документам целиком, и если важно:
- быстро поднять локальный поиск внутри приложения или сервиса;
- учитывать опечатки пользователя;
- учитывать фонетические ошибки при вводе фамилий;
- не падать исключениями в ожидаемых рабочих сценариях.

## Текущее состояние API

Библиотека использует Result-based контракт:
- `PrepareIndexResult(...)`
- `FindResult(...)`

Новые интеграции следует строить только на этих методах.

### Параллельное построение индекса

По умолчанию библиотека использует консервативный порог автоматической
параллельной обработки. Это снижает давление на память и уменьшает вероятность
пауз GC на небольших и средних наборах данных.

Для desktop-приложений рекомендуется строить индекс в фоне и не выполнять
индексацию на UI-потоке.

Для серверных сценариев, где важнее скорость построения индекса, параллельную
обработку можно включить явно:

```csharp
await search.PrepareIndexResult(
    source,
    forceParallel: true,
    parallelProcessingThreshold: 0);

## Сборка

```bash
dotnet build
dotnet test
dotnet pack src/SearchEngine/SearchEngine.csproj -c Release
```

## Лицензия

Проект распространяется по лицензии MIT.
Файл `LICENSE` включается в репозиторий и в NuGet-пакет.
