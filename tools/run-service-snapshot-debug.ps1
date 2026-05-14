$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src/SearchEngine.Service/SearchEngine.Service.csproj"
$snapshotPath = Join-Path $repoRoot "src/SearchEngine.Service/data/debug-search-index-snapshot.json"

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:SearchEngineService__Snapshot__IsEnabled = "true"
$env:SearchEngineService__Snapshot__AutoRestoreOnStart = "true"
$env:SearchEngineService__Snapshot__FilePath = $snapshotPath

Write-Host "Запуск SearchEngine.Service в snapshot-debug режиме"
Write-Host "Snapshot file: $snapshotPath"

dotnet run --project $projectPath