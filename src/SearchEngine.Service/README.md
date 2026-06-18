# SearchEngine.Service

`SearchEngine.Service` — лёгкий HTTP-сервис поверх библиотеки `Ti-Soft.SearchEngine`.

Сервис предназначен для сценариев, где нужен простой встроенный поисковый API без развёртывания Elasticsearch, Lucene-сервера или другой отдельной поисковой платформы.

## Текущий статус

Сервис находится в стадии MVP.

Сейчас реализовано:

- проверка работоспособности сервиса;
- Docker healthcheck контейнера через endpoint `/health`;
- проверка готовности индекса к поиску;
- локальная нагрузочная smoke-проверка сервиса;
- получение информации о версии сервиса и версии библиотеки;
- проверка запроса на построение индекса;
- построение in-memory индекса;
- получение состояния текущего индекса;
- просмотр безопасного описания заранее настроенных источников данных;
- встроенный `in-memory` provider источника данных;
- SQLite provider источника данных;
- локальный SQLite demo-сценарий без внешней БД;
- SQLite demo-сценарий в Docker-контейнере;
- PostgreSQL provider источника данных;
- локальный PostgreSQL demo-сценарий через Docker-контейнер;
- построение индекса из зарегистрированного provider-а источника данных;
- простой поиск по текущему индексу;
- получение активных настроек сервиса;
- получение допустимых параметров поиска;
- точный поиск;
- нечёткий поиск;
- поиск с учётом места совпадения в слове;
- фонетический поиск;
- поиск русских фамилий в латинской записи;
- ограничение количества документов и длины текста через конфигурацию;
- сохранение snapshot индекса в файл;
- ручное восстановление индекса из snapshot-файла;
- автоматическое восстановление индекса из snapshot-файла при старте сервиса.

По умолчанию индекс хранится только в памяти процесса.

Если включён snapshot, сервис может сохранить исходные документы индекса в файл и затем восстановить индекс через `POST /v1/index/restore`.

Если дополнительно включён `AutoRestoreOnStart`, сервис пытается восстановить индекс из snapshot-файла при старте. Если snapshot-файл отсутствует или повреждён, сервис продолжает запуск, а индекс остаётся неготовым до ручного построения или восстановления.

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
docker pull ghcr.io/titeha/searchengine-service:0.8.0
```

Запустить контейнер:

```powershell
docker run --rm -p 8080:8080 ghcr.io/titeha/searchengine-service:0.8.0
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

Для проверки SQLite provider-а в контейнере см. раздел `SQLite demo-сценарий в Docker-контейнере`.

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
ghcr.io/titeha/searchengine-service:0.8.0
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
  "serviceVersion": "0.8.0.0",
  "status": "ok",
  "searchEngineVersion": "2.0.1.0"
}
```

Версии могут отображаться в формате сборки, например `0.8.0.0` для сервиса и `2.0.1.0` для библиотеки.

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
    "autoRestoreOnStart": false,
    "filePath": "data/search-index-snapshot.json"
  }
}
```

Если контейнер запущен с переопределёнными переменными окружения:

```powershell
docker run --rm -p 8080:8080 `
  -e SearchEngineService__MaxDocumentCount=1 `
  -e SearchEngineService__MaxDocumentTextLength=5 `
  ghcr.io/titeha/searchengine-service:0.8.0
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
  "supportedProviders": [
    "in-memory",
    "postgres",
    "sqlite"
  ],
  "items": []
}
```

Сейчас сервис содержит встроенные provider-ы:

| Provider | Назначение |
|---|---|
| `in-memory` | локальный demo-provider с документами из конфигурации |
| `sqlite` | provider для чтения документов из SQLite-БД |
| `postgres` | provider для чтения документов из PostgreSQL-БД |
| `firebird` | provider для чтения документов из Firebird-БД |
| `sqlserver` | provider для чтения документов из Microsoft SQL Server |
| `mysql` | provider для чтения документов из MySQL или MariaDB |
| `oracle` | provider для чтения документов из Oracle Database |

Для СУБД без встроенного provider-а (например IBM DB2) и других источников см. раздел «Расширение источников данных».

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

На текущем этапе подключены provider-ы `in-memory`, `sqlite`, `postgres`, `firebird`, `sqlserver`, `mysql` и `oracle`.

Для нестандартных источников (например IBM DB2) см. раздел «Расширение источников данных».

### Требования к профилю источника данных

Каждый профиль источника данных должен содержать provider.

Минимальная структура профиля:

```json
{
  "SearchEngineService": {
    "Sources": {
      "source-name": {
        "IsEnabled": true,
        "Provider": "sqlite"
      }
    }
  }
}
```

Если профиль настроен с provider-ом, для которого reader не зарегистрирован, поле `isProviderSupported` в ответе `GET /v1/data-sources` будет `false`, пока соответствующий reader не добавлен в сервис.

Если Provider не указан или содержит только пробелы, endpoint POST /v1/index/from-source вернёт ошибку:

```json
{
  "code": "DataSourceProviderIsEmpty",
  "message": "Для источника данных не указан provider: source-name."
}
```

Для разных provider-ов могут быть дополнительные обязательные настройки.

## SQLite provider

SQLite provider позволяет построить индекс из локальной SQLite-БД.

Provider использует заранее настроенный профиль источника данных.

Пример конфигурации:

```json
{
  "SearchEngineService": {
    "Sources": {
      "sqlite-demo": {
        "IsEnabled": true,
        "Provider": "sqlite",
        "ConnectionStringName": "SQLITE_DEMO",
        "Query": "select id, text from search_documents order by id"
      }
    }
  },
  "ConnectionStrings": {
    "SQLITE_DEMO": "Data Source=data/sqlite-demo/search-demo.db;Pooling=False"
  }
}
```

SQL-запрос должен вернуть две колонки:

| Колонка | Описание |
|---|---|
| `id` | целочисленный идентификатор документа |
| `text` | текст документа для индексации |

Endpoint `GET /v1/data-sources` не возвращает SQL-запрос и строку подключения. Он показывает только безопасное описание профиля.

Для SQLite provider-а обязательны:

- `ConnectionStringName`;
- `Query`.

`ConnectionStringName` — это имя строки подключения. Сервис сначала ищет его в секции `ConnectionStrings`, а если не находит, пробует прочитать как обычный конфигурационный ключ или environment variable.

`Query` должен вернуть две колонки:

| Колонка | Описание |
|---|---|
| `id` | целочисленный идентификатор документа |
| `text` | текст документа для индексации |

## SQLite demo-сценарий

Для локальной проверки SQLite provider-а не нужно устанавливать внешнюю БД.

В репозитории есть seed-инструмент:

```text
tools/SearchEngine.Service.SqliteDemoSeed
```

Он создаёт локальную SQLite-БД с demo-документами:

- `Иванов Сергей Петрович`;
- `Папандопуло Александр`;
- `Красный велосипед`.

Для запуска полного demo-сценария используется скрипт:

```text
tools/run-service-sqlite-demo.ps1
```

Запуск из корня репозитория:

```powershell
.\tools\run-service-sqlite-demo.ps1
```

Если порт `5037` занят:

```powershell
.\tools\run-service-sqlite-demo.ps1 -Url "http://localhost:5040"
```

Скрипт выполняет два действия:

1. создаёт локальную SQLite-БД;
2. запускает сервис с окружением `SqliteDemo`.

Сервис подхватывает файл:

```text
src/SearchEngine.Service/appsettings.SqliteDemo.json
```

Для ручной проверки есть отдельный `.http`-файл:

```text
src/SearchEngine.Service/SearchEngine.Service.SqliteDemo.http
```

Запросы в нём нужно выполнять сверху вниз.

Сначала проверяется фонетический сценарий:

1. `POST /v1/index/from-source` с `isPhoneticSearch = true`;
2. поиск `Ivanov` должен найти `id = 1`;
3. поиск `Papandopulo` должен найти `id = 2`.

Затем для проверки поиска внутри слова индекс перестраивается без фонетики:

1. `POST /v1/index/from-source` с `isPhoneticSearch = false`;
2. поиск `лосип` с `searchLocation = InWord` должен найти `id = 3`.

Такой порядок нужен потому, что фонетический индекс использует phonetic keys, а не обычные строковые ключи слов.

## SQLite demo-сценарий в Docker-контейнере

SQLite provider можно проверить не только локальным запуском сервиса, но и через опубликованный Docker-образ.

Для этого в репозитории есть скрипт:

```text
tools/run-service-sqlite-demo-container.ps1
```

Скрипт выполняет следующие действия:

1. создаёт локальную SQLite-БД с demo-документами;
2. запускает опубликованный Docker-образ сервиса;
3. монтирует папку с SQLite-БД внутрь контейнера;
4. передаёт строку подключения через environment variable;
5. запускает сервис с окружением `SqliteDemo`.

Запуск из корня репозитория:

```powershell
.\tools\run-service-sqlite-demo-container.ps1
```

По умолчанию используется образ:

```text
ghcr.io/titeha/searchengine-service:0.8.0
```

И порт:

```text
8080
```

Если порт `8080` занят:

```powershell
.\tools\run-service-sqlite-demo-container.ps1 -Port 8090
```

Можно явно указать образ:

```powershell
.\tools\run-service-sqlite-demo-container.ps1 `
  -Image "ghcr.io/titeha/searchengine-service:0.8.0" `
  -Port 8080
```

Скрипт создаёт SQLite-БД локально:

```text
src/SearchEngine.Service/data/sqlite-demo-container/search-demo.db
```

Эта папка добавлена в `.gitignore`, поэтому локальная demo-БД не должна попадать в Git.

Внутри контейнера БД монтируется как:

```text
/data/search-demo.db
```

Строка подключения передаётся так:

```text
ConnectionStrings__SQLITE_DEMO=Data Source=/data/search-demo.db;Mode=ReadOnly;Pooling=False
```

Для ручной проверки есть отдельный `.http`-файл:

```text
src/SearchEngine.Service/SearchEngine.Service.SqliteDemo.Container.http
```

Запросы в нём нужно выполнять сверху вниз.

Ожидаемый сценарий:

1. `GET /health` возвращает `200 OK`;
2. `GET /v1/info` показывает версию сервиса;
3. `GET /v1/data-sources` показывает источник `sqlite-demo`;
4. `POST /v1/index/from-source` строит индекс из SQLite-БД;
5. `GET /ready` возвращает `200 OK`;
6. поиск `Ivanov` находит `id = 1`;
7. поиск `Papandopulo` находит `id = 2`;
8. после перестроения индекса без фонетики поиск `лосип` с `InWord` находит `id = 3`.

Для проверки фонетики индекс строится с `isPhoneticSearch = true`.

Для проверки поиска внутри слова индекс перестраивается с `isPhoneticSearch = false`, потому что фонетический индекс использует phonetic keys, а не обычные строковые ключи слов.

## PostgreSQL provider

PostgreSQL provider позволяет построить индекс из PostgreSQL-БД.

Provider использует заранее настроенный профиль источника данных.

Пример конфигурации:

```json
{
  "SearchEngineService": {
    "Sources": {
      "postgres-demo": {
        "IsEnabled": true,
        "Provider": "postgres",
        "ConnectionStringName": "POSTGRES_DEMO",
        "Query": "select id, text from search_documents order by id"
      }
    }
  },
  "ConnectionStrings": {
    "POSTGRES_DEMO": "Host=localhost;Port=55432;Database=search_demo;Username=search;Password=search;Pooling=false"
  }
}
```

Для PostgreSQL provider-а обязательны:

- `ConnectionStringName`;
- `Query`.

`ConnectionStringName` — это имя строки подключения. Сервис сначала ищет его в секции `ConnectionStrings`, а если не находит, пробует прочитать как обычный конфигурационный ключ или environment variable.

SQL-запрос должен вернуть две колонки:

| Колонка | Описание |
|---|---|
| `id` | целочисленный идентификатор документа |
| `text` | текст документа для индексации |

Endpoint `GET /v1/data-sources` не возвращает SQL-запрос и строку подключения. Он показывает только безопасное описание профиля.

## PostgreSQL demo-сценарий

Для локальной проверки PostgreSQL provider-а не нужно устанавливать PostgreSQL вручную.

В репозитории есть скрипт:

```text
tools/run-service-postgres-demo.ps1
```

Скрипт выполняет следующие действия:

1. запускает временный PostgreSQL-контейнер;
2. создаёт demo-БД;
3. загружает данные из `tools/postgres-demo/seed.sql`;
4. запускает `SearchEngine.Service` с окружением `PostgresDemo`;
5. передаёт строку подключения через environment variable.

Запуск из корня репозитория:

```powershell
.\tools\run-service-postgres-demo.ps1
```

Если порт сервиса `5037` занят:

```powershell
.\tools\run-service-postgres-demo.ps1 -Url "http://localhost:5040"
```

Если порт PostgreSQL `55432` занят:

```powershell
.\tools\run-service-postgres-demo.ps1 -PostgresPort 55433
```

По умолчанию используется образ:

```text
postgres:16-alpine
```

Можно указать другой образ:

```powershell
.\tools\run-service-postgres-demo.ps1 -PostgresImage "postgres:17-alpine"
```

Сервис подхватывает файл:

```text
src/SearchEngine.Service/appsettings.PostgresDemo.json
```

Demo-данные лежат в файле:

```text
tools/postgres-demo/seed.sql
```

Для ручной проверки есть отдельный `.http`-файл:

```text
src/SearchEngine.Service/SearchEngine.Service.PostgresDemo.http
```

Запросы в нём нужно выполнять сверху вниз.

Ожидаемый сценарий:

1. `GET /health` возвращает `200 OK`;
2. `GET /v1/config` возвращает активную конфигурацию;
3. `GET /v1/data-sources` показывает источник `postgres-demo`;
4. `POST /v1/index/from-source` строит индекс из PostgreSQL;
5. `GET /ready` возвращает `200 OK`;
6. поиск `Ivanov` находит `id = 1`;
7. поиск `Papandopulo` находит `id = 2`;
8. после перестроения индекса без фонетики поиск `лосип` с `InWord` находит `id = 3`.

Для проверки фонетики индекс строится с `isPhoneticSearch = true`.

Для проверки поиска внутри слова индекс перестраивается с `isPhoneticSearch = false`, потому что фонетический индекс использует phonetic keys, а не обычные строковые ключи слов.

После остановки скрипта PostgreSQL demo-контейнер удаляется.

## Firebird provider

Firebird provider позволяет построить индекс из Firebird-БД. Он добавлен в первую очередь, потому что часть существующих проектов использует Firebird.

Provider использует заранее настроенный профиль источника данных.

Пример конфигурации:

```json
{
  "SearchEngineService": {
    "Sources": {
      "firebird-demo": {
        "IsEnabled": true,
        "Provider": "firebird",
        "ConnectionStringName": "FIREBIRD_DEMO",
        "Query": "select id, text from search_documents order by id"
      }
    }
  },
  "ConnectionStrings": {
    "FIREBIRD_DEMO": "DataSource=localhost;Port=3050;Database=/firebird/data/search_demo.fdb;User=SYSDBA;Password=masterkey;Charset=UTF8"
  }
}
```

Для Firebird provider-а обязательны:

- `ConnectionStringName`;
- `Query`.

`ConnectionStringName` — это имя строки подключения. Сервис сначала ищет его в секции `ConnectionStrings`, а если не находит, пробует прочитать как обычный конфигурационный ключ или environment variable.

SQL-запрос должен вернуть две колонки:

| Колонка | Описание |
|---|---|
| `id` | целочисленный идентификатор документа |
| `text` | текст документа для индексации |

Firebird-источник использует тот же общий механизм SQL-чтения, что SQLite и PostgreSQL (таймаут команды, лимит чтения, проверка колонок `id`/`text`). Reader Firebird-провайдера покрыт unit-тестами (имя provider-а и валидация профиля); сквозное SQL-чтение проверяется интеграционными тестами SQLite, так как все SQL-reader-ы используют общий базовый класс. Отдельный Firebird demo-сценарий требует внешнего Firebird-сервера и будет добавлен отдельным шагом.

## SQL Server provider

SQL Server provider позволяет построить индекс из Microsoft SQL Server.

Provider использует заранее настроенный профиль источника данных.

Пример конфигурации:

```json
{
  "SearchEngineService": {
    "Sources": {
      "sqlserver-demo": {
        "IsEnabled": true,
        "Provider": "sqlserver",
        "ConnectionStringName": "SQLSERVER_DEMO",
        "Query": "select id, text from search_documents order by id"
      }
    }
  },
  "ConnectionStrings": {
    "SQLSERVER_DEMO": "Server=localhost,1433;Database=search_demo;User Id=search;Password=Str0ng!Passw0rd;Encrypt=True;TrustServerCertificate=True"
  }
}
```

Для SQL Server provider-а обязательны:

- `ConnectionStringName`;
- `Query`.

SQL-запрос должен вернуть две колонки `id` (целочисленный идентификатор) и `text` (текст для индексации).

SQL Server-источник использует общий механизм SQL-чтения (таймаут команды, лимит чтения, проверка колонок `id`/`text`). Reader покрыт unit-тестами (имя provider-а и валидация профиля); сквозное SQL-чтение проверяется интеграционными тестами SQLite, так как все SQL-reader-ы используют общий базовый класс.

## MySQL / MariaDB provider

MySQL provider позволяет построить индекс из MySQL или совместимой MariaDB. Используется ADO.NET-провайдер MySqlConnector.

Provider использует заранее настроенный профиль источника данных.

Пример конфигурации:

```json
{
  "SearchEngineService": {
    "Sources": {
      "mysql-demo": {
        "IsEnabled": true,
        "Provider": "mysql",
        "ConnectionStringName": "MYSQL_DEMO",
        "Query": "select id, text from search_documents order by id"
      }
    }
  },
  "ConnectionStrings": {
    "MYSQL_DEMO": "Server=localhost;Port=3306;Database=search_demo;User Id=search;Password=search;"
  }
}
```

Для MySQL provider-а обязательны:

- `ConnectionStringName`;
- `Query`.

SQL-запрос должен вернуть две колонки `id` (целочисленный идентификатор) и `text` (текст для индексации).

MySQL-источник использует общий механизм SQL-чтения (таймаут команды, лимит чтения, проверка колонок `id`/`text`). Reader покрыт unit-тестами (имя provider-а и валидация профиля); сквозное SQL-чтение проверяется интеграционными тестами SQLite, так как все SQL-reader-ы используют общий базовый класс.

## Oracle provider

Oracle provider позволяет построить индекс из Oracle Database. Используется управляемый ADO.NET-провайдер Oracle.ManagedDataAccess.Core.

Provider использует заранее настроенный профиль источника данных.

Пример конфигурации:

```json
{
  "SearchEngineService": {
    "Sources": {
      "oracle-demo": {
        "IsEnabled": true,
        "Provider": "oracle",
        "ConnectionStringName": "ORACLE_DEMO",
        "Query": "select id, text from search_documents order by id"
      }
    }
  },
  "ConnectionStrings": {
    "ORACLE_DEMO": "User Id=search;Password=search;Data Source=localhost:1521/XEPDB1"
  }
}
```

Для Oracle provider-а обязательны:

- `ConnectionStringName`;
- `Query`.

SQL-запрос должен вернуть две колонки `id` (целочисленный идентификатор) и `text` (текст для индексации). Имена колонок сопоставляются без учёта регистра, поэтому стандартные для Oracle имена в верхнем регистре (`ID`, `TEXT`) тоже подходят.

Oracle-источник использует общий механизм SQL-чтения (таймаут команды, лимит чтения, проверка колонок `id`/`text`). Reader покрыт unit-тестами (имя provider-а и валидация профиля); сквозное SQL-чтение проверяется интеграционными тестами SQLite, так как все SQL-reader-ы используют общий базовый класс.

## Безопасность DB-источников

Сервис не принимает строку подключения и SQL-запрос от внешнего клиента.

Внешний клиент передаёт только имя заранее настроенного источника данных:

```json
{
  "sourceName": "products",
  "isPhoneticSearch": true
}
```

Профиль источника данных настраивается на стороне сервиса:

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

Строка подключения должна храниться отдельно, например в `ConnectionStrings` или в переменной окружения:

```powershell
-e ConnectionStrings__PRODUCTS_DB="Host=...;Database=...;Username=...;Password=..."
```

Рекомендуемая модель безопасности:

- использовать отдельного пользователя БД только для поискового сервиса;
- выдавать этому пользователю только read-only доступ;
- разрешать доступ только к нужным таблицам или view;
- не использовать учётную запись владельца схемы;
- не использовать административную учётную запись;
- не передавать connection string во внешний HTTP API;
- не передавать SQL-запрос во внешний HTTP API;
- хранить SQL-запрос только в конфигурации сервиса;
- возвращать наружу только безопасное описание профиля через `GET /v1/data-sources`.

Endpoint `GET /v1/data-sources` намеренно не возвращает:

- строку подключения;
- значение секрета;
- SQL-запрос;
- пароль;
- параметры доступа к БД.

Он показывает только признаки наличия настроек:

```json
{
  "name": "products",
  "isEnabled": true,
  "provider": "postgres",
  "isProviderSupported": true,
  "hasConnectionStringName": true,
  "hasQuery": true
}
```

## Рекомендации по SQL-запросам источников данных

SQL-запрос источника должен возвращать две колонки:

| Колонка | Описание |
|---|---|
| `id` | целочисленный идентификатор документа |
| `text` | текст документа для индексации |

Пример:

```sql
select
    id,
    name as text
from products
where is_active = true;
```

Рекомендации:

- использовать простой `select`;
- явно указывать колонки `id` и `text`;
- ограничивать набор индексируемых данных через `where`;
- использовать view, если запрос становится сложным;
- не использовать изменяющие операции `insert`, `update`, `delete`, `drop`, `alter`;
- не давать сервису права на изменение данных;
- не строить SQL из пользовательского ввода.

На текущем этапе сервис выполняет заранее заданный SQL-запрос из конфигурации. Внешний пользователь не может передать свой SQL через API.

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

## Настройки через Docker environment variables

При запуске контейнера настройки можно переопределить через переменные окружения.

Пример:

```powershell
docker run --rm -p 8080:8080 `
  -e SearchEngineService__MaxDocumentCount=50000 `
  -e SearchEngineService__MaxDocumentTextLength=20000 `
  ghcr.io/titeha/searchengine-service:0.8.0
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
  ghcr.io/titeha/searchengine-service:0.8.0
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

## Защита от перегрузки (DoS)

Сервис ограничивает нагрузку извне двумя независимыми механизмами.

**Лимит размера тела запроса.** Запрос с телом больше допустимого отклоняется с кодом `413` ещё до чтения тела (по заголовку `Content-Length`), поэтому слишком большой запрос не загружается в память. Лимит дополнительно применяется на уровне Kestrel.

**Ограничение частоты запросов.** Слишком частые запросы от одного клиента отклоняются с кодом `429`. Используется окно фиксированной длины, клиент определяется по IP-адресу.

Оба механизма включены по умолчанию и настраиваются:

```json
{
  "SearchEngineService": {
    "Limits": {
      "MaxRequestBodyBytes": 33554432,
      "RateLimit": {
        "IsEnabled": true,
        "PermitLimit": 240,
        "WindowSeconds": 1,
        "QueueLimit": 0
      }
    }
  }
}
```

| Параметр | Описание | Значение по умолчанию |
|---|---|---|
| `Limits:MaxRequestBodyBytes` | максимальный размер тела запроса в байтах | `33554432` (32 МиБ) |
| `Limits:RateLimit:IsEnabled` | включить ограничение частоты запросов | `true` |
| `Limits:RateLimit:PermitLimit` | число запросов от одного клиента за окно | `240` |
| `Limits:RateLimit:WindowSeconds` | длительность окна в секундах | `1` |
| `Limits:RateLimit:QueueLimit` | число запросов в очереди сверх лимита | `0` |

При построении очень больших индексов через `POST /v1/index` значение `MaxRequestBodyBytes` можно увеличить. Если сервис работает за обратным прокси, для корректного ограничения по IP нужно пробрасывать реальный адрес клиента (`X-Forwarded-For` и forwarded headers).

## Несколько индексов

Сервис поддерживает несколько независимых именованных индексов. Это удобно, когда одно приложение строит индексы из разных источников (например, отдельный индекс по товарам и отдельный по ФИО) или из локальных файлов.

Имя индекса передаётся параметром:

- в теле запроса (`index`) для `POST /v1/index`, `POST /v1/index/from-source`, `POST /v1/search`;
- в query-параметре (`?index=`) для `GET /v1/index`, `GET /ready`, `POST /v1/index/restore`.

Если имя индекса не задано, используется индекс по умолчанию с именем `default`.

Допустимые символы имени индекса — латинские буквы, цифры, `-` и `_` (до 64 символов). Имя используется для формирования имени snapshot-файла, поэтому другие символы отклоняются с ошибкой `InvalidIndexName`.

Индексы изолированы: построение одного индекса не блокирует поиск и построение других. Глобального лока нет — построение одного индекса сериализуется lock-free шлюзом, а чтение выполняется без блокировок.

Если для индекса уже идёт построение, повторный запрос на построение того же индекса не отклоняется: индекс просто остаётся в состоянии `Building`, а инициатор достраивает его.

Список известных индексов:

```http
GET {{host}}/v1/indexes
```

Пример ответа:

```json
{
  "items": [
    {
      "indexName": "default",
      "state": "Ready",
      "isReady": true,
      "documentCount": 3,
      "searchableDocumentCount": 3,
      "isPhoneticSearch": false,
      "createdAtUtc": "..."
    },
    {
      "indexName": "products",
      "state": "Ready",
      "isReady": true,
      "documentCount": 100,
      "searchableDocumentCount": 100,
      "isPhoneticSearch": false,
      "createdAtUtc": "..."
    }
  ]
}
```

Snapshot хранится отдельно для каждого индекса: индекс по умолчанию использует базовый файл из настроек, а именованные индексы — файлы с именем индекса перед расширением, например `search-index-snapshot.products.json`. Автоматическое восстановление при старте сейчас восстанавливает только индекс по умолчанию; именованные индексы восстанавливаются по запросу через `POST /v1/index/restore?index=имя`.

## Состояние индекса

```http
GET {{host}}/v1/index
```

Параметр `index` (query) выбирает индекс. Без него возвращается состояние индекса по умолчанию.

Поле `state` показывает состояние индекса: `NotBuilt`, `Building` или `Ready`.

До построения индекса ответ будет примерно таким:

```json
{
  "indexName": "default",
  "state": "NotBuilt",
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

Если у источника не указан provider:

```json
{
  "code": "DataSourceProviderIsEmpty",
  "message": "Для источника данных не указан provider: products."
}
```

Если для SQLite или PostgreSQL источника не указано имя строки подключения:

```json
{
  "code": "DataSourceConnectionStringNameIsEmpty",
  "message": "Для SQLite-источника не указано имя строки подключения: sqlite-demo."
}
```

Если для SQLite или PostgreSQL источника не указан SQL-запрос:

```json
{
  "code": "DataSourceQueryIsEmpty",
  "message": "Для SQLite-источника не указан SQL-запрос: sqlite-demo."
}
```

Если provider указан, но для него не зарегистрирован reader внутри сервиса:

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

Endpoint работает только с provider-ами, зарегистрированными внутри сервиса.

Если профиль настроен, но provider не зарегистрирован, сервис вернёт ошибку `DataSourceProviderNotSupported`.

Перед чтением данных сервис выполняет базовую проверку профиля:

1. проверяет, что имя источника передано;
2. проверяет, что источник существует;
3. проверяет, что источник включён;
4. проверяет, что указан `Provider`;
5. проверяет обязательные поля для конкретного provider-а;
6. проверяет, что provider поддерживается текущим сервисом;
7. вызывает reader источника данных;
8. строит индекс из полученных документов.

## Demo in-memory источник данных

Для локальной проверки построения индекса из источника данных не нужно подключать БД и не нужно менять основной `appsettings.json`.

В репозитории есть отдельный demo-конфиг:

```text
src/SearchEngine.Service/appsettings.DemoSource.json
```

Он содержит источник данных demo с provider-ом in-memory и тремя документами:

- Иванов Сергей Петрович;
- Папандопуло Александр;
- Красный велосипед.

Для запуска demo-сценария используется скрипт:

```text
tools/run-service-demo-source.ps1
```

Запуск из корня репозитория:

```powershell
.\tools\run-service-demo-source.ps1
```

Если порт 5037 занят:

```powershell
.\tools\run-service-demo-source.ps1 -Url "<http://localhost:5040>"
```

Скрипт запускает сервис с окружением:

```text
ASPNETCORE_ENVIRONMENT=DemoSource
```

Поэтому сервис подхватывает файл:

```text
appsettings.DemoSource.json
```

После запуска можно использовать отдельный .http-файл:

```text
src/SearchEngine.Service/SearchEngine.Service.DemoSource.http
```

Запросы в этом файле нужно выполнять сверху вниз:

1. GET /health;
2. GET /v1/config;
3. GET /v1/data-sources;
4. GET /v1/index;
5. POST /v1/index/from-source;
6. GET /v1/index;
7. GET /ready;
8. POST /v1/search.

Ожидаемый результат после POST /v1/index/from-source:

```json
{
  "isReady": true,
  "documentCount": 3,
  "searchableDocumentCount": 3,
  "isPhoneticSearch": true,
  "createdAtUtc": "..."
}
```

После этого запрос:

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

должен найти документ с id = 1.

Запрос:

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

должен найти документ с id = 2.

Запрос:

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

должен найти документ с id = 3.

Demo in-memory источник нужен только для локальной проверки и демонстрации полного сценария без внешней БД.

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

- поддерживает несколько именованных индексов (см. раздел «Несколько индексов»);
- строит индекс из документов, переданных во входящем JSON;
- строит индекс из заранее настроенного источника данных через `POST /v1/index/from-source`;
- может сохранять snapshot индекса в файл;
- может вручную восстанавливать индекс из snapshot-файла;
- автоматическое восстановление индекса при старте доступно только при включённом snapshot и `AutoRestoreOnStart`;
- умеет читать конфигурационные профили источников данных;
- есть встроенные provider-ы `in-memory`, `sqlite`, `postgres`, `firebird`, `sqlserver`, `mysql` и `oracle`;
- SQLite provider проверяется локально и через Docker demo-сценарий;
- SQL-provider-ы Firebird, SQL Server, MySQL/MariaDB и Oracle покрыты unit-тестами; сквозное SQL-чтение проверяется интеграционными тестами SQLite на общем базовом reader-е;
- сервис не подключается к БД без заранее зарегистрированного reader-а;
- внешний API не принимает connection string и SQL-запрос;
- безопасность DB-доступа зависит от корректной настройки read-only пользователя БД;
- не отслеживает изменения в источнике данных;
- не содержит авторизации;
- Dockerfile используется для локальной и CI-сборки контейнера;
- контейнер публикуется в GHCR только по релизному тегу `service-v*`;
- полноценная high-load-валидация production-сценариев пока не проводилась.

Эти возможности будут добавляться отдельными шагами.

## Локальная нагрузочная smoke-проверка

В репозитории есть локальный console-инструмент для быстрой smoke-проверки производительности сервиса:

```text
tools/SearchEngine.Service.LoadSmoke
```

Это не полноценный benchmark и не high-load-валидация. Инструмент нужен, чтобы быстро проверить, что сервис отвечает под параллельной нагрузкой и что изменения в коде не привели к очевидной деградации.

Перед запуском smoke-теста нужно запустить `SearchEngine.Service`.

Пример запуска:

```powershell
dotnet run --project .\tools\SearchEngine.Service.LoadSmoke\SearchEngine.Service.LoadSmoke.csproj -- `
  --url http://localhost:5037 `
  --documents 10000 `
  --parallel 16 `
  --seconds 15
```

Параметры:

| Параметр | Описание |
|---|---|
| `--url` | адрес запущенного сервиса |
| `--documents` | количество тестовых документов для построения индекса |
| `--parallel` | количество параллельных worker-ов |
| `--seconds` | длительность поисковой нагрузки |

Smoke-тест выполняет следующие действия:

1. проверяет `/health`;
2. строит тестовый индекс через `POST /v1/index`;
3. выполняет параллельные поисковые запросы через `POST /v1/search`;
4. считает количество успешных запросов;
5. считает ошибки;
6. выводит RPS и задержки `p50`, `p95`, `p99`.

Пример вывода:

```text
SearchEngine.Service load smoke
URL:        http://localhost:5037
Documents:  10000
Parallel:   16
Duration:   15s

Building test index...
Index built in 374 ms

Running search load...

Result:
Success: 406474
Errors:  0
RPS:     27098.27
p50:     0 ms
p95:     1 ms
p99:     2 ms
```

Результаты зависят от машины, режима запуска, размера индекса и типа запросов. Эти цифры не являются гарантией производительности в production.

## Расширение источников данных

Сервис уже умеет строить индекс из зарегистрированных provider-ов через `POST /v1/index/from-source`.

Сейчас встроены provider-ы:

- `in-memory`;
- `sqlite`;
- `postgres`;
- `firebird`;
- `sqlserver`;
- `mysql`;
- `oracle`.

Для СУБД без встроенного reader-а (например IBM DB2) используйте публичный API расширения (см. ниже).

Для БД, файлов и других источников, которых нет в стандартной поставке сервиса, есть публичный API регистрации reader-ов. Любой reader берёт данные из заранее настроенного профиля, возвращает пары `id` / `text` и не выводит наружу connection string, SQL-запрос или другие секреты.

### Кастомная СУБД через фабрику подключения

Если СУБД работает через ADO.NET (`DbConnection`), отдельный класс писать не нужно. Достаточно зарегистрировать источник по имени provider-а и фабрике подключения. Так подключаются СУБД без встроенного reader-а, например IBM DB2:

```csharp
using IBM.Data.Db2; // ADO.NET-провайдер конкретной СУБД

builder.Services.AddSqlSearchDataSource(
    provider: "db2",
    providerDisplayName: "IBM DB2",
    connectionFactory: connectionString => new DB2Connection(connectionString));
```

После этого в профиле источника достаточно указать `"Provider": "db2"`. Вся механика SQL-чтения (строка подключения, таймаут, лимит чтения, проверка колонок `id`/`text`) берётся из общего базового reader-а, так же как для `sqlite` и `postgres`.

### Полностью собственный reader

Если источник данных не SQL (файл, внешний API, очередь), нужно реализовать интерфейс `ISearchDataSourceReader` и зарегистрировать его:

```csharp
public sealed class FileSearchDataSourceReader : ISearchDataSourceReader
{
    public string Provider => "file";

    public ApiError? ValidateProfile(string sourceName, SearchDataSourceOptions options) => null;

    public Task<IReadOnlyList<SearchDataSourceDocument>> ReadAsync(
        string sourceName,
        SearchDataSourceOptions options,
        CancellationToken cancellationToken = default)
    {
        // прочитать данные источника и вернуть пары id/text
    }
}

builder.Services.AddSearchDataSourceReader<FileSearchDataSourceReader>();
```

Требования к собственному reader-у:

- `Provider` — уникальное имя, по которому источник выбирается в профиле;
- `ReadAsync` возвращает пары `id` / `text` из заранее настроенного профиля;
- reader не принимает connection string или SQL-запрос от внешнего клиента;
- секреты (строка подключения, пароль) не возвращаются наружу.

Оба метода регистрации (`AddSqlSearchDataSource` и `AddSearchDataSourceReader<T>`) добавляют reader в общий registry, поэтому он сразу виден в `GET /v1/data-sources` и доступен в `POST /v1/index/from-source`.

Динамическая загрузка внешних reader-ов как плагинов пока не реализована: пользовательский reader подключается в собственной сборке сервиса при его композиции.

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

1. сделать валидацию профилей источников данных расширяемой;
2. вынести общее чтение SQL-источников, чтобы новые DB-provider-ы не дублировали один и тот же код;
3. добавить timeout-ы и лимиты чтения из SQL-источников;
4. описать API для создания пользовательских reader-ов источников данных;
5. добавить Firebird provider источника данных;
6. добавить Firebird demo-сценарий и тесты;
7. добавить базовую защиту API;
8. подготовить сервис к нескольким поисковым индексам;
9. усилить snapshot-механику и сделать сохранение более устойчивым к сбоям;
10. расширить наблюдаемость, нагрузочные проверки и документацию по production-развёртыванию контейнера.

Отдельная цель всех следующих шагов — сохранять код сопровождаемым: небольшие классы с понятной ответственностью, явные границы между endpoint-ами, настройками, reader-ами и индексом, минимальное дублирование, понятные имена и тесты на новое поведение.
