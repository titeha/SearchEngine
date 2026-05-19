param(
    [string]$Url = "http://localhost:5037"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$serviceProjectPath = Join-Path $repoRoot "src/SearchEngine.Service/SearchEngine.Service.csproj"
$seedProjectPath = Join-Path $repoRoot "tools/SearchEngine.Service.SqliteDemoSeed/SearchEngine.Service.SqliteDemoSeed.csproj"
$databasePath = Join-Path $repoRoot "src/SearchEngine.Service/data/sqlite-demo/search-demo.db"

$environmentKeys = @(
    "ASPNETCORE_ENVIRONMENT",
    "ASPNETCORE_URLS",
    "ConnectionStrings__SQLITE_DEMO"
)

$previousValues = @{}

foreach ($key in $environmentKeys) {
    $previousValues[$key] = [Environment]::GetEnvironmentVariable($key, "Process")
}

try {
    Write-Host "Подготовка SQLite demo database"
    Write-Host "Database: $databasePath"
    Write-Host ""

    dotnet run --project $seedProjectPath -- --database $databasePath

    $env:ASPNETCORE_ENVIRONMENT = "SqliteDemo"
    $env:ASPNETCORE_URLS = $Url
    $env:ConnectionStrings__SQLITE_DEMO = "Data Source=$databasePath;Pooling=False"

    Write-Host ""
    Write-Host "Запуск SearchEngine.Service с SQLite demo источником данных"
    Write-Host "URL: $Url"
    Write-Host "Environment: SqliteDemo"
    Write-Host "Источник данных: sqlite-demo"
    Write-Host ""
    Write-Host "Проверка источника:"
    Write-Host "GET  $Url/v1/data-sources"
    Write-Host ""
    Write-Host "Построение индекса из источника:"
    Write-Host "POST $Url/v1/index/from-source"
    Write-Host ""

    dotnet run --project $serviceProjectPath --no-launch-profile
}
finally {
    foreach ($key in $environmentKeys) {
        $previousValue = $previousValues[$key]

        if ($null -eq $previousValue) {
            [Environment]::SetEnvironmentVariable($key, $null, "Process")
        }
        else {
            [Environment]::SetEnvironmentVariable($key, $previousValue, "Process")
        }
    }
}