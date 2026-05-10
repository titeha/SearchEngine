# SearchEngine.Service

`SearchEngine.Service` — лёгкий HTTP-сервис поверх библиотеки `Ti-Soft.SearchEngine`.

Сервис предназначен для сценариев, где нужен простой встроенный поисковый API без развёртывания Elasticsearch, Lucene-сервера или другой отдельной поисковой платформы.

## Текущий статус

Сервис находится в стадии MVP.

Сейчас реализовано:

- проверка работоспособности сервиса;
- проверка готовности индекса к поиску;
- получение информации о версии сервиса и версии библиотеки;
- проверка запроса на построение индекса;
- построение in-memory индекса;
- получение состояния текущего индекса;
- простой поиск по текущему индексу;
- точный поиск;
- нечёткий поиск;
- поиск с учётом места совпадения в слове;
- фонетический поиск;
- поиск русских фамилий в латинской записи;
- получение допустимых параметров поиска.

Индекс хранится только в памяти процесса. После перезапуска сервиса индекс нужно построить заново.

## Запуск из Visual Studio

В Visual Studio нужно выбрать проект `SearchEngine.Service` как стартовый:

```text
Solution Explorer
→ правый клик по SearchEngine.Service
→ Set as Startup Project
```

Затем запустить сервис:

```text
Ctrl + F5
```

После запуска Visual Studio откроет локальный адрес сервиса, например:

```text
http://localhost:5037/health
```

Порт может отличаться.

## Локальный запуск через Docker

Сервис можно собрать и запустить в локальном Docker-контейнере.

Сборку нужно выполнять из корня репозитория, где находится файл `SearchEngine.sln`.

```powershell
docker build -t searchengine-service:dev -f .\src\SearchEngine.Service\Dockerfile .
```

Запуск контейнера:

```powershell
docker run --rm -p 8080:8080 searchengine-service:dev
```

После запуска сервис будет доступен по адресу:

```text
http://localhost:8080
```

## Проверка работоспособности

Endpoint `/health` показывает, что процесс сервиса запущен и может принимать HTTP-запросы.

Он не проверяет, построен ли поисковый индекс.

```http
GET {{host}}/health
```

Ожидаемый ответ:

```json
{
  "status": "ok"
}
```

Этот endpoint удобно использовать для проверки запуска контейнера.

## Проверка готовности к поиску

Endpoint `/ready` показывает, готов ли сервис выполнять поиск.

Сервис считается готовым к поиску только после успешного построения индекса через `POST /v1/index`.

```http
GET {{host}}/ready
```

До построения индекса endpoint возвращает HTTP `503 Service Unavailable`.

Пример ответа до построения индекса:

```json
{
  "status": "not_ready",
  "isReady": false,
  "documentCount": 0,
  "searchableDocumentCount": 0,
  "isPhoneticSearch": false,
  "createdAtUtc": null
}
```

После построения индекса endpoint возвращает HTTP `200 OK`.

Пример ответа после построения индекса:

```json
{
  "status": "ready",
  "isReady": true,
  "documentCount": 3,
  "searchableDocumentCount": 3,
  "isPhoneticSearch": true,
  "createdAtUtc": "..."
}
```

Разница между endpoint-ами:

| Endpoint | Что проверяет | Когда возвращает успешный ответ |
|---|---|---|
| `/health` | процесс сервиса запущен | сразу после запуска сервиса |
| `/ready` | индекс построен и поиск готов | после успешного `POST /v1/index` |

## Проверка локального Docker-контейнера

Для проверки запуска контейнера используется `/health`, потому что после старта контейнера индекс ещё не построен.

Endpoint `/ready` до построения индекса вернёт `503 Service Unavailable`, и это нормальное поведение.

Информация о сервисе доступна по адресу:

```text
http://localhost:8080/v1/info
```

Остановить контейнер можно сочетанием клавиш:

```text
Ctrl + C
```

## Запуск опубликованного контейнера

Опубликованный Docker-образ сервиса доступен в GitHub Container Registry.

Загрузить образ:

```powershell
docker pull ghcr.io/titeha/searchengine-service:0.2.0
```

Запустить контейнер:

```powershell
docker run --rm -p 8080:8080 ghcr.io/titeha/searchengine-service:0.2.0
```

После запуска сервис будет доступен по адресу:

```text
http://localhost:8080
```

Проверка работоспособности:

```text
http://localhost:8080/health
```

Ожидаемый ответ:

```json
{
  "status": "ok"
}
```

Для проверки запуска контейнера используется `/health`, потому что после старта контейнера индекс ещё не построен.

Endpoint `/ready` до построения индекса вернёт `503 Service Unavailable`, и это нормальное поведение.

Информация о сервисе:

```text
http://localhost:8080/v1/info
```

Актуальный образ также публикуется с тегом:

```text
ghcr.io/titeha/searchengine-service:latest
```

Для воспроизводимого запуска лучше использовать версионный тег:

```text
ghcr.io/titeha/searchengine-service:0.2.0
```

## Проверка через `.http`-файл

В проекте есть файл:

```text
SearchEngine.Service.http
```

В начале файла нужно указать фактический адрес сервиса:

```http
@host = http://localhost:5037
```

Если сервис запущен на другом порту, нужно заменить порт на свой.

Важно: если сервис слушает `http`, в `.http`-файле тоже должен быть `http`, а не `https`.

## Проверка работоспособности через `.http`

```http
GET {{host}}/health
```

Ожидаемый ответ:

```json
{
  "status": "ok"
}
```

## Информация о сервисе

```http
GET {{host}}/v1/info
```

Пример ответа:

```json
{
  "service": "TiSoft.SearchEngine.Service",
  "serviceVersion": "0.2.0.0",
  "status": "ok",
  "searchEngineVersion": "2.0.1.0"
}
```

Версии могут отображаться в формате сборки, например `0.2.0.0` для сервиса и `2.0.1.0` для библиотеки.

## Допустимые параметры поиска

Endpoint возвращает допустимые значения параметров поиска и значения по умолчанию.

```http
GET {{host}}/v1/search/options
```

Пример ответа:

```json
{
  "matchModes": [
    "AllTerms",
    "AnyTerm",
    "SoftAllTerms"
  ],
  "searchTypes": [
    "ExactSearch",
    "NearSearch"
  ],
  "searchLocations": [
    "BeginWord",
    "InWord"
  ],
  "defaultMatchMode": "AllTerms",
  "defaultSearchType": "ExactSearch",
  "defaultSearchLocation": "BeginWord"
}
```

Параметр `matchMode` управляет тем, как объединяются слова поискового запроса:

| Значение | Описание |
|---|---|
| `AllTerms` | все слова запроса должны быть найдены |
| `AnyTerm` | достаточно найти хотя бы одно слово запроса |
| `SoftAllTerms` | мягкий режим объединения слов запроса |

Параметр `searchType` задаёт тип поиска:

| Значение | Описание |
|---|---|
| `ExactSearch` | точный поиск |
| `NearSearch` | нечёткий поиск с учётом опечаток |

Параметр `searchLocation` задаёт место совпадения внутри слова:

| Значение | Описание |
|---|---|
| `BeginWord` | совпадение с начала слова |
| `InWord` | совпадение внутри слова |

## Состояние индекса

```http
GET {{host}}/v1/index
```

До построения индекса ответ будет примерно таким:

```json
{
  "isReady": false,
  "documentCount": 0,
  "searchableDocumentCount": 0,
  "isPhoneticSearch": false,
  "createdAtUtc": null
}
```

## Проверка документов перед индексацией

```http
POST {{host}}/v1/index/validate
Content-Type: application/json

{
  "isPhoneticSearch": true,
  "documents": [
    {
      "id": 1,
      "text": "Иванов Сергей Петрович"
    },
    {
      "id": 2,
      "text": "Папандопуло Александр"
    },
    {
      "id": 3,
      "text": ""
    }
  ]
}
```

Ожидаемый ответ:

```json
{
  "documentCount": 3,
  "searchableDocumentCount": 2,
  "isPhoneticSearch": true
}
```

## Построение индекса

```http
POST {{host}}/v1/index
Content-Type: application/json

{
  "isPhoneticSearch": true,
  "documents": [
    {
      "id": 1,
      "text": "Иванов Сергей Петрович"
    },
    {
      "id": 2,
      "text": "Папандопуло Александр"
    },
    {
      "id": 3,
      "text": "Красный велосипед"
    }
  ]
}
```

Ожидаемый ответ:

```json
{
  "isReady": true,
  "documentCount": 3,
  "searchableDocumentCount": 3,
  "isPhoneticSearch": true,
  "createdAtUtc": "..."
}
```

## Точный поиск

По умолчанию поиск использует:

- `matchMode`: `AllTerms`;
- `searchType`: `ExactSearch`;
- `searchLocation`: `BeginWord`.

Поэтому для простого точного поиска достаточно передать только поисковую строку.

Минимальный запрос:

```http
POST {{host}}/v1/search
Content-Type: application/json

{
  "query": "Иванов"
}
```

```http
POST {{host}}/v1/search
Content-Type: application/json

{
  "query": "Иванов",
  "matchMode": "AllTerms",
  "searchType": "ExactSearch",
  "searchLocation": "BeginWord"
}
```

Ожидаемый результат: найден документ с `id = 1`.

## Поиск внутри слова

```http
POST {{host}}/v1/search
Content-Type: application/json

{
  "query": "лосип",
  "matchMode": "AllTerms",
  "searchType": "ExactSearch",
  "searchLocation": "InWord"
}
```

Ожидаемый результат: найден документ с `id = 3`, потому что строка входит в слово `велосипед`.

## Нечёткий поиск

```http
POST {{host}}/v1/search
Content-Type: application/json

{
  "query": "Иваноы",
  "matchMode": "AllTerms",
  "searchType": "NearSearch",
  "searchLocation": "BeginWord",
  "acceptableCountMisprint": 1
}
```

Ожидаемый результат: найден документ с `id = 1`.

## Нечёткий поиск через процент точности

```http
POST {{host}}/v1/search
Content-Type: application/json

{
  "query": "веласипед",
  "matchMode": "AllTerms",
  "searchType": "NearSearch",
  "searchLocation": "BeginWord",
  "precisionSearch": 70
}
```

Ожидаемый результат: найден документ с `id = 3`.

## Фонетический поиск русской фамилии в латинской записи

Для этой проверки индекс должен быть построен с параметром:

```json
{
  "isPhoneticSearch": true
}
```

Запрос:

```http
POST {{host}}/v1/search
Content-Type: application/json

{
  "query": "Ivanov",
  "matchMode": "AllTerms",
  "searchType": "ExactSearch",
  "searchLocation": "BeginWord"
}
```

Ожидаемый результат: найден документ с `id = 1`.

## Фонетический поиск фамилии Папандопуло

```http
POST {{host}}/v1/search
Content-Type: application/json

{
  "query": "Papandopulo",
  "matchMode": "AllTerms",
  "searchType": "ExactSearch",
  "searchLocation": "BeginWord"
}
```

Ожидаемый результат: найден документ с `id = 2`.

Также должен работать вариант:

```http
POST {{host}}/v1/search
Content-Type: application/json

{
  "query": "Papondopulo",
  "matchMode": "AllTerms",
  "searchType": "ExactSearch",
  "searchLocation": "BeginWord"
}
```

Ожидаемый результат: найден документ с `id = 2`.

## Ограничения текущего MVP

Сейчас сервис:

- хранит индекс только в памяти;
- поддерживает один общий индекс;
- не сохраняет snapshot индекса на диск;
- не содержит авторизации;
- Dockerfile используется для локальной и CI-сборки контейнера;
- контейнер публикуется в GHCR только по релизному тегу `service-v*`;
- не предназначен для production high-load сценариев.

Эти возможности будут добавляться отдельными шагами.

## План развития

Ближайшие шаги:

1. добавить сохранение и восстановление индекса;
2. добавить настройки сервиса через конфигурацию;
3. добавить базовую защиту API;
4. добавить нагрузочные проверки сервиса;
5. добавить документацию по развёртыванию контейнера.
