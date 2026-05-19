param(
    [string]$Url = "http://localhost:5037"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src/SearchEngine.Service/SearchEngine.Service.csproj"

$environmentKeys = @(
    "ASPNETCORE_ENVIRONMENT",
    "ASPNETCORE_URLS"
)

$previousValues = @{}

foreach ($key in $environmentKeys) {
    $previousValues[$key] = [Environment]::GetEnvironmentVariable($key, "Process")
}

try {
    $env:ASPNETCORE_ENVIRONMENT = "DemoSource"
    $env:ASPNETCORE_URLS = $Url

    Write-Host "Запуск SearchEngine.Service с demo in-memory источником данных"
    Write-Host "URL: $Url"
    Write-Host "Environment: DemoSource"
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