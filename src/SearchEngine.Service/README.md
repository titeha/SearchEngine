# SearchEngine.Service

`SearchEngine.Service` — лёгкий HTTP-сервис поверх библиотеки `Ti-Soft.SearchEngine`.

Сервис предназначен для сценариев, где нужен простой встроенный поисковый API без развёртывания Elasticsearch, Lucene-сервера или другой отдельной поисковой платформы.

## Текущий статус

Сервис находится в стадии MVP.

Сейчас реализовано:

- проверка работоспособности сервиса;
- получение информации о сервисе и версии библиотеки;
- проверка запроса на построение индекса;
- построение in-memory индекса;
- получение состояния текущего индекса;
- простой поиск по текущему индексу;
- точный поиск;
- нечёткий поиск;
- поиск с учётом места совпадения в слове;
- фонетический поиск;
- поиск русских фамилий в латинской записи.

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

Информация о сервисе:

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
docker pull ghcr.io/titeha/searchengine-service:0.1.0
```

Запустить контейнер:

```powershell
docker run --rm -p 8080:8080 ghcr.io/titeha/searchengine-service:0.1.0
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
ghcr.io/titeha/searchengine-service:0.1.0
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

## Проверка работоспособности

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
  "status": "ok",
  "searchEngineVersion": "2.0.1.0"
}
```

Версия может отображаться как `2.0.1.0`, потому что это версия сборки.

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
- Dockerfile добавлен только для локальной сборки;
- контейнер публикуется в GHCR только по релизному тегу `service-v*`;
- не предназначен для production high-load сценариев.

Эти возможности будут добавляться отдельными шагами.

## План развития

Ближайшие шаги:

1. убрать блокировки из hot path поиска через immutable snapshot;
2. добавить сохранение и восстановление индекса;
3. добавить настройки сервиса через конфигурацию;
4. добавить базовую защиту API;
5. добавить нагрузочные проверки сервиса.