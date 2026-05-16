# SearchEngine.Service

`SearchEngine.Service` — лёгкий HTTP-сервис поверх библиотеки `Ti-Soft.SearchEngine`.

Сервис предназначен для сценариев, где нужен простой встроенный поисковый API без развёртывания Elasticsearch, Lucene-сервера или другой отдельной поисковой платформы.

## Текущий статус

Сервис находится в стадии MVP.

Сейчас реализовано:

- проверка работоспособности сервиса;
- Docker healthcheck контейнера через endpoint `/health`;
- проверка готовности индекса к поиску;
- получение информации о версии сервиса и версии библиотеки;
- проверка запроса на построение индекса;
- построение in-memory индекса;
- получение состояния текущего индекса;
- просмотр безопасного описания заранее настроенных источников данных;
- построение индекса из зарегистрированного provider-а источника данных;
- простой поиск по текущему индексу;
- получение активных настроек сервиса;
- просмотр безопасного описания заранее настроенных источников данных;
- получение допустимых параметров поиска;
- точный поиск;
- нечёткий поиск;
- поиск с учётом места совпадения в слове;
- фонетический поиск;
- поиск русских фамилий в латинской записи;
- ограничение количества документов и длины текста через конфигурацию;
- сохранение snapshot индекса в файл;
- ручное восстановление индекса из snapshot-файла.

По умолчанию индекс хранится только в памяти процесса.

Если включён snapshot, сервис может сохранить исходные документы индекса в файл и затем восстановить индекс через `POST /v1/index/restore`.

Автоматическое восстановление индекса при старте сервиса пока не выполняется.

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

## Локальный snapshot-debug запуск

Для проверки сохранения и автоматического восстановления snapshot индекса не нужно вручную менять `appsettings.json`.

В репозитории есть отдельный PowerShell-скрипт:

```text
tools/run-service-snapshot-debug.ps1
```

Запускать его нужно из корня репозитория:

```powershell
.\tools\run-service-snapshot-debug.ps1
```

Скрипт временно задаёт переменные окружения только для текущего процесса запуска:

```text
SearchEngineService__Snapshot__IsEnabled=true
SearchEngineService__Snapshot__AutoRestoreOnStart=true
SearchEngineService__Snapshot__FilePath=src/SearchEngine.Service/data/debug-search-index-snapshot.json
```

После запуска можно проверить активную конфигурацию:

```http
GET {{host}}/v1/config
```

Ожидаемый фрагмент ответа:

```json
{
  "snapshot": {
    "isEnabled": true,
    "autoRestoreOnStart": true,
    "filePath": "..."
  }
}
```

Проверочный сценарий:

1. запустить сервис через tools/run-service-snapshot-debug.ps1;
2. выполнить POST /v1/index;
3. убедиться, что создан snapshot-файл;
4. остановить сервис;
5. снова запустить сервис через тот же скрипт;
6. выполнить GET /ready;
7. убедиться, что сервис вернул 200 OK;
8. выполнить поиск, например POST /v1/search с запросом Ivanov.

Snapshot-файл создаётся локально и не должен попадать в Git. Для этого путь src/SearchEngine.Service/data/ добавлен в .gitignore.

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

Для проверки Docker `HEALTHCHECK` контейнер удобно запустить с именем:

```powershell
docker run -d --rm --name searchengine-service-healthcheck-test -p 8080:8080 searchengine-service:dev
```

Проверить состояние:

```powershell
docker inspect --format "{{.State.Health.Status}}" searchengine-service-healthcheck-test
```

Остановить контейнер:

```powershell
docker stop searchengine-service-healthcheck-test
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

## Docker HEALTHCHECK

Docker-образ сервиса содержит встроенный `HEALTHCHECK`.

Проверка контейнера выполняется через endpoint `/health`, а не через `/ready`.

Причина:

- `/health` проверяет, что процесс сервиса запущен и отвечает на HTTP-запросы;
- `/ready` проверяет, что поисковый индекс уже построен;
- после старта контейнера индекс ещё не построен, поэтому `/ready` может возвращать `503 Service Unavailable`;
- для Docker это не должно считаться ошибкой запуска контейнера.

Проверить состояние контейнера можно командой:

```powershell
docker ps
```

В колонке `STATUS` после успешной проверки должно появиться состояние:

```text
healthy
```

Также можно проверить состояние напрямую:

```powershell
docker inspect --format "{{.State.Health.Status}}" searchengine-service-healthcheck-test
```

Ожидаемый результат:

```text
healthy
```

## Запуск опубликованного контейнера

Опубликованный Docker-образ сервиса доступен в GitHub Container Registry.

Загрузить образ:

```powershell
docker pull ghcr.io/titeha/searchengine-service:0.4.0
```

Запустить контейнер:

```powershell
docker run --rm -p 8080:8080 ghcr.io/titeha/searchengine-service:0.4.0
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
ghcr.io/titeha/searchengine-service:0.4.0
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
  "serviceVersion": "0.4.0.0",
  "status": "ok",
  "searchEngineVersion": "2.0.1.0"
}
```

Версии могут отображаться в формате сборки, например `0.4.0.0` для сервиса и `2.0.1.0` для библиотеки.

## Активная конфигурация сервиса

Endpoint возвращает фактические настройки ограничений, с которыми сейчас работает сервис.

Это полезно, если настройки были переопределены через `appsettings.json` или Docker environment variables.

```http
GET {{host}}/v1/config
```

Пример ответа со стандартными настройками:

```json
{
  "maxDocumentCount": 100000,
  "maxDocumentTextLength": 10000,
  "snapshot": {
    "isEnabled": false,
    "filePath": "data/search-index-snapshot.json"
  }
}
```

Если контейнер запущен с переопределёнными переменными окружения:

```powershell
docker run --rm -p 8080:8080 `
  -e SearchEngineService__MaxDocumentCount=1 `
  -e SearchEngineService__MaxDocumentTextLength=5 `
  ghcr.io/titeha/searchengine-service:0.4.0
```

то endpoint вернёт:

```json
{
  "maxDocumentCount": 1,
  "maxDocumentTextLength": 5,
  "snapshot": {
    "isEnabled": false,
    "filePath": "data/search-index-snapshot.json"
  }
}
```

## Источники данных

Сервис поддерживает заранее настроенные профили источников данных.

Профили задаются в конфигурации сервиса, а не передаются внешним клиентом в момент запроса.

Это сделано намеренно: внешний клиент не должен передавать произвольную строку подключения или SQL-запрос.

```http
GET {{host}}/v1/data-sources
```

При стандартной конфигурации источники данных не заданы.

Пример ответа:

```json
{
  "supportedProviders": [],
  "items": []
}
```

Пример настройки профиля источника данных:

```json
{
  "SearchEngineService": {
    "Sources": {
      "products": {
        "IsEnabled": true,
        "Provider": "postgres",
        "ConnectionStringName": "PRODUCTS_DB",
        "Query": "select id, name as text from products where is_active = true"
      }
    }
  }
}
```

Endpoint `GET /v1/data-sources` возвращает только безопасное описание профилей.

Пример ответа:

```json
{
  "supportedProviders": [],
  "items": [
    {
      "name": "products",
      "isEnabled": true,
      "provider": "postgres",
      "isProviderSupported": false,
      "hasConnectionStringName": true,
      "hasQuery": true
    }
  ]
}
```

Поле `isProviderSupported` показывает, есть ли в сервисе зарегистрированный reader для указанного provider-а.

Endpoint не возвращает:

- строку подключения;
- значение секрета со строкой подключения;
- SQL-запрос;
- параметры доступа к БД.

На текущем этапе реальные provider-ы БД ещё не подключены. Поэтому профиль может быть описан в конфигурации, но `isProviderSupported` будет `false`, пока соответствующий reader не добавлен в сервис.

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

## Настройки сервиса

Сервис поддерживает настройки ограничений для построения индекса.

Настройки задаются в секции `SearchEngineService`.

Пример `appsettings.json`:

```json
{
  "SearchEngineService": {
    "MaxDocumentCount": 100000,
    "MaxDocumentTextLength": 10000,
    "Snapshot": {
      "IsEnabled": false,
      "AutoRestoreOnStart": false,
      "FilePath": "data/search-index-snapshot.json"
    },
    "Sources": {}
  }
}
```

Параметры:

| Параметр | Описание | Значение по умолчанию |
|---|---|---|
| `MaxDocumentCount` | максимальное количество документов для построения индекса | `100000` |
| `MaxDocumentTextLength` | максимальная длина текста одного документа | `10000` |


Если количество документов превышает MaxDocumentCount, endpoint POST /v1/index возвращает ошибку:

```json
{
  "code": "TooManyDocuments",
  "message": "Количество документов превышает допустимое значение: 100000."
}
```

Если текст документа длиннее MaxDocumentTextLength, endpoint POST /v1/index возвращает ошибку:

```json
{
  "code": "DocumentTextTooLong",
  "message": "Длина текста документа превышает допустимое значение: 10000."
}
```

Текущие значения этих настроек можно проверить через endpoint `GET /v1/config`.

Ответ будет примерно таким:

```json
{
  "maxDocumentCount": 100000,
  "maxDocumentTextLength": 10000,
  "snapshot": {
    "isEnabled": false,
    "autoRestoreOnStart": false,
    "filePath": "data/search-index-snapshot.json"
  }
}
```

## Источники данных

Сервис поддерживает конфигурационные профили источников данных.

Пока сервис только показывает безопасное описание настроенных профилей. Построение индекса из БД будет добавлено отдельным шагом.

```http
GET {{host}}/v1/data-sources
```

При стандартной конфигурации источники данных не заданы.

Пример ответа:

```json
{
  "items": []
}
```

Пример настройки профиля источника данных:

```json
{
  "SearchEngineService": {
    "Sources": {
      "products": {
        "IsEnabled": true,
        "Provider": "postgres",
        "ConnectionStringName": "PRODUCTS_DB",
        "Query": "select id, name as text from products where is_active = true"
      }
    }
  }
}
```

Endpoint `GET /v1/data-sources` возвращает только безопасное описание профилей.

Пример ответа:

```json
{
  "items": [
    {
      "name": "products",
      "isEnabled": true,
      "provider": "postgres",
      "hasConnectionStringName": true,
      "hasQuery": true
    }
  ]
}
```

Endpoint не возвращает:

- строку подключения;
- имя переменной окружения со строкой подключения как секрет;
- SQL-запрос;
- параметры доступа к БД.

Это сделано намеренно: профиль источника данных должен быть настроен на стороне сервиса, а внешний клиент должен работать только с именем заранее разрешённого профиля.

## Настройки через Docker environment variables

При запуске контейнера настройки можно переопределить через переменные окружения.

Пример:

```powershell
docker run --rm -p 8080:8080 `
  -e SearchEngineService__MaxDocumentCount=50000 `
  -e SearchEngineService__MaxDocumentTextLength=20000 `
  ghcr.io/titeha/searchengine-service:0.4.0
```

Для вложенных настроек используется двойное подчёркивание `__`.

Например:

```text
SearchEngineService__MaxDocumentCount
```

соответствует настройке:

```text
SearchEngineService:MaxDocumentCount
```

## Snapshot индекса

Snapshot позволяет сохранить данные, из которых можно восстановить поисковый индекс после перезапуска сервиса.

Сервис сохраняет не внутреннюю структуру индекса, а исходные документы и настройки индексации:

- версию snapshot-формата;
- признак включения фонетического поиска;
- дату и время создания snapshot;
- список документов `id` / `text`.

Snapshot по умолчанию выключен.

Пример настройки:

```json
{
  "SearchEngineService": {
    "Snapshot": {
      "IsEnabled": true,
      "AutoRestoreOnStart": false,
      "FilePath": "data/search-index-snapshot.json"
    }
  }
}
```

Параметр `AutoRestoreOnStart` включает автоматическое восстановление индекса из snapshot-файла при старте сервиса.

Если `AutoRestoreOnStart = true`, но snapshot-файл отсутствует или повреждён, сервис не падает. Он продолжает запуск, а индекс остаётся неготовым до ручного построения или восстановления.

Через Docker environment variables:

```powershell
docker run --rm -p 8080:8080 `
  -e SearchEngineService__Snapshot__IsEnabled=true `
  -e SearchEngineService__Snapshot__AutoRestoreOnStart=true `
  -e SearchEngineService__Snapshot__FilePath=data/search-index-snapshot.json `
  ghcr.io/titeha/searchengine-service:0.4.0
```

Для вложенных настроек используется двойное подчёркивание `__`.

Например:

```text
SearchEngineService__Snapshot__IsEnabled
```

соответствует настройке:

```text
SearchEngineService:Snapshot:IsEnabled
```

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

## Построение индекса из источника данных

Endpoint строит индекс из заранее настроенного источника данных.

```http
POST {{host}}/v1/index/from-source
Content-Type: application/json

{
  "sourceName": "products",
  "isPhoneticSearch": true
}
```

Параметры:

| Параметр | Описание |
|---|---|
| `sourceName` | имя заранее настроенного источника данных |
| `isPhoneticSearch` | признак включения фонетического поиска |

Если имя источника не передано, сервис вернёт ошибку:

```json
{
  "code": "EmptySourceName",
  "message": "Не указано имя источника данных."
}
```

Если источник не найден:

```json
{
  "code": "DataSourceNotFound",
  "message": "Источник данных не найден: products."
}
```

Если источник отключён:

```json
{
  "code": "DataSourceDisabled",
  "message": "Источник данных отключён: products."
}
```

Если provider источника пока не поддерживается сервисом:

```json
{
  "code": "DataSourceProviderNotSupported",
  "message": "Provider источника данных не поддерживается: postgres."
}
```

Если provider зарегистрирован и успешно вернул документы, сервис построит индекс и вернёт состояние индекса:

```json
{
  "isReady": true,
  "documentCount": 2,
  "searchableDocumentCount": 2,
  "isPhoneticSearch": true,
  "createdAtUtc": "..."
}
```

После этого можно выполнять обычный поиск через `POST /v1/search`.

## Восстановление индекса из snapshot

Endpoint восстанавливает поисковый индекс из snapshot-файла.

```http
POST {{host}}/v1/index/restore
```

Если snapshot выключен, endpoint вернёт ошибку:

```json
{
  "code": "SnapshotDisabled",
  "message": "Восстановление snapshot поискового индекса отключено."
}
```

Если snapshot включён, но файл отсутствует, endpoint вернёт ошибку:

```json
{
  "code": "SnapshotNotFound",
  "message": "Snapshot-файл поискового индекса не найден."
}
```

Если snapshot-файл найден и успешно прочитан, сервис построит индекс заново из сохранённых документов.

Пример успешного ответа:

```json
{
  "isReady": true,
  "documentCount": 3,
  "searchableDocumentCount": 3,
  "isPhoneticSearch": true,
  "createdAtUtc": "..."
}
```

После восстановления можно проверить готовность:

```http
GET {{host}}/ready
```

И выполнить обычный поиск:

```http
POST {{host}}/v1/search
Content-Type: application/json

{
  "query": "Ivanov"
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

- поддерживает один общий индекс;
- строит индекс из документов, переданных во входящем JSON;
- может сохранять snapshot индекса в файл;
- может вручную восстанавливать индекс из snapshot-файла;
- автоматическое восстановление индекса при старте доступно только при включённом snapshot и `AutoRestoreOnStart`;
- умеет читать конфигурационные профили источников данных;
- умеет строить индекс из зарегистрированного provider-а источника данных;
- реальные provider-ы БД пока не подключены;
- не подключается к БД без заранее зарегистрированного reader-а;
- не отслеживает изменения в источнике данных;
- не содержит авторизации;
- Dockerfile используется для локальной и CI-сборки контейнера;
- контейнер публикуется в GHCR только по релизному тегу `service-v*`;
- не предназначен для production high-load сценариев.

Эти возможности будут добавляться отдельными шагами.

## Построение индекса из источников данных

Сейчас сервис уже умеет читать конфигурационные профили источников данных и показывать их безопасное описание через `GET /v1/data-sources`.

Следующий этап — добавить построение индекса по имени заранее настроенного источника данных.

Возможный сценарий:

1. сервис получает имя заранее настроенного источника данных;
2. сервис подключается к БД с read-only доступом;
3. выполняет заранее заданный SQL-запрос;
4. получает список пар `id` / `text`;
5. строит поисковый индекс;
6. обновляет состояние готовности через `/ready`.

Пример будущей идеи конфигурации:

```json
{
  "sources": {
    "products": {
      "provider": "postgres",
      "connectionStringName": "PRODUCTS_DB",
      "query": "select id, name as text from products where is_active = true"
    }
  }
}
```

Публичный API не должен принимать произвольную connection string и произвольный SQL-запрос от внешнего клиента.

Безопаснее использовать заранее настроенные профили источников данных, а из API передавать только имя профиля.

## Планируемая актуализация индекса

Также планируется исследовать способы актуализации индекса при изменении данных в БД.

Возможные варианты:

- ручная перестройка индекса через API;
- периодическая перестройка по расписанию;
- polling по полю updated_at;
- чтение таблицы изменений;
- интеграция с outbox-паттерном;
- использование механизмов change tracking / CDC, если они доступны и безопасны для конкретной БД.

На первом этапе предпочтительнее простая и безопасная схема:

1. ручная перестройка индекса;
2. затем периодическая перестройка;
3. затем аккуратный мониторинг изменений.

Мониторинг изменений в БД должен добавляться только после проработки безопасности, прав доступа и поведения при ошибках подключения.

## План развития

Ближайшие шаги:

1. добавить первый реальный provider источника данных;
2. добавить безопасное подключение к БД с read-only доступом;
3. добавить ручную перестройку индекса по имени источника данных;
4. исследовать мониторинг изменений в БД для актуализации индекса;
5. добавить базовую защиту API;
6. добавить нагрузочные проверки сервиса;
7. добавить документацию по развёртыванию контейнера.
