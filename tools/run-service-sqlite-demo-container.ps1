param(
    [string]$Image = "ghcr.io/titeha/searchengine-service:0.6.0",
    [int]$Port = 8080
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$seedProjectPath = Join-Path $repoRoot "tools/SearchEngine.Service.SqliteDemoSeed/SearchEngine.Service.SqliteDemoSeed.csproj"
$databasePath = Join-Path $repoRoot "src/SearchEngine.Service/data/sqlite-demo-container/search-demo.db"
$databaseDirectory = Split-Path -Parent $databasePath
$containerDatabasePath = "/data/search-demo.db"

Write-Host "Подготовка SQLite demo database для Docker-контейнера"
Write-Host "Database: $databasePath"
Write-Host ""

dotnet run --project $seedProjectPath -- --database $databasePath

Write-Host ""
Write-Host "Запуск Docker-контейнера SearchEngine.Service"
Write-Host "Image: $Image"
Write-Host "URL:   http://localhost:$Port"
Write-Host "SQLite file inside container: $containerDatabasePath"
Write-Host ""
Write-Host "Проверка источника:"
Write-Host "GET  http://localhost:$Port/v1/data-sources"
Write-Host ""
Write-Host "Построение индекса из источника:"
Write-Host "POST http://localhost:$Port/v1/index/from-source"
Write-Host ""

docker run --rm `
    -p "$Port`:8080" `
    -e ASPNETCORE_ENVIRONMENT=SqliteDemo `
    -e "ConnectionStrings__SQLITE_DEMO=Data Source=$containerDatabasePath;Mode=ReadOnly;Pooling=False" `
    --mount "type=bind,source=$databaseDirectory,target=/data,readonly" `
    $Image