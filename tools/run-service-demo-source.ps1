param(
    [string]$Url = "http://localhost:5037"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src/SearchEngine.Service/SearchEngine.Service.csproj"

$environmentKeys = @(
    "ASPNETCORE_ENVIRONMENT",
    "ASPNETCORE_URLS",

    "SearchEngineService__Sources__demo__IsEnabled",
    "SearchEngineService__Sources__demo__Provider",

    "SearchEngineService__Sources__demo__Documents__0__Id",
    "SearchEngineService__Sources__demo__Documents__0__Text",

    "SearchEngineService__Sources__demo__Documents__1__Id",
    "SearchEngineService__Sources__demo__Documents__1__Text",

    "SearchEngineService__Sources__demo__Documents__2__Id",
    "SearchEngineService__Sources__demo__Documents__2__Text"
)

$previousValues = @{}

foreach ($key in $environmentKeys) {
    $previousValues[$key] = [Environment]::GetEnvironmentVariable($key, "Process")
}

try {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = $Url

    $env:SearchEngineService__Sources__demo__IsEnabled = "true"
    $env:SearchEngineService__Sources__demo__Provider = "in-memory"

    $env:SearchEngineService__Sources__demo__Documents__0__Id = "1"
    $env:SearchEngineService__Sources__demo__Documents__0__Text = "Иванов Сергей Петрович"

    $env:SearchEngineService__Sources__demo__Documents__1__Id = "2"
    $env:SearchEngineService__Sources__demo__Documents__1__Text = "Папандопуло Александр"

    $env:SearchEngineService__Sources__demo__Documents__2__Id = "3"
    $env:SearchEngineService__Sources__demo__Documents__2__Text = "Красный велосипед"

    Write-Host "Запуск SearchEngine.Service с demo in-memory источником данных"
    Write-Host "URL: $Url"
    Write-Host "Источник данных: demo"
    Write-Host ""
    Write-Host "Проверка источника:"
    Write-Host "GET  $Url/v1/data-sources"
    Write-Host ""
    Write-Host "Построение индекса из источника:"
    Write-Host "POST $Url/v1/index/from-source"
    Write-Host ""

    dotnet run --project $projectPath --no-launch-profile
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