param(
    [string]$Url = "http://localhost:5037",
    [int]$PostgresPort = 55432,
    [string]$PostgresImage = "postgres:16-alpine"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$serviceProjectPath = Join-Path $repoRoot "src/SearchEngine.Service/SearchEngine.Service.csproj"
$seedSqlPath = Join-Path $repoRoot "tools/postgres-demo/seed.sql"

$containerName = "searchengine-postgres-demo"
$databaseName = "search_demo"
$databaseUser = "search"
$databasePassword = "search"

$environmentKeys = @(
    "ASPNETCORE_ENVIRONMENT",
    "ASPNETCORE_URLS",
    "ConnectionStrings__POSTGRES_DEMO"
)

$previousValues = @{}

foreach ($key in $environmentKeys) {
    $previousValues[$key] = [Environment]::GetEnvironmentVariable($key, "Process")
}

function Remove-DemoContainerIfExists {
    $existingContainer = docker ps -aq --filter "name=^/$containerName$"

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to check existing PostgreSQL demo container."
    }

    if ([string]::IsNullOrWhiteSpace($existingContainer)) {
        return
    }

    docker rm -f $containerName | Out-Null

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to remove existing PostgreSQL demo container."
    }
}

function Wait-PostgresReady {
    for ($i = 0; $i -lt 60; $i++) {
        try {
            docker exec $containerName pg_isready -U $databaseUser -d $databaseName *> $null

            if ($LASTEXITCODE -eq 0) {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 1
    }

    throw "PostgreSQL demo container did not become ready."
}

try {
    if (!(Test-Path $seedSqlPath)) {
        throw "Seed SQL file not found: $seedSqlPath"
    }

    Write-Host "Removing old PostgreSQL demo container if it exists"
    Remove-DemoContainerIfExists

    Write-Host "Starting PostgreSQL demo container"
    Write-Host "Image: $PostgresImage"
    Write-Host "Port:  $PostgresPort"
    Write-Host ""

    docker run -d `
        --name $containerName `
        -p "$PostgresPort`:5432" `
        -e POSTGRES_DB=$databaseName `
        -e POSTGRES_USER=$databaseUser `
        -e POSTGRES_PASSWORD=$databasePassword `
        $PostgresImage | Out-Null

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start PostgreSQL demo container."
    }

    Write-Host "Waiting for PostgreSQL readiness"
    Wait-PostgresReady

    Write-Host "Seeding PostgreSQL demo data"

    docker cp $seedSqlPath "${containerName}:/tmp/seed.sql"

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to copy seed SQL into PostgreSQL container."
    }

    docker exec `
        -e PGPASSWORD=$databasePassword `
        $containerName `
        psql `
        -U $databaseUser `
        -d $databaseName `
        -f /tmp/seed.sql | Out-Null

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to seed PostgreSQL demo data."
    }

    $connectionString = "Host=localhost;Port=$PostgresPort;Database=$databaseName;Username=$databaseUser;Password=$databasePassword;Pooling=false"

    $env:ASPNETCORE_ENVIRONMENT = "PostgresDemo"
    $env:ASPNETCORE_URLS = $Url
    $env:ConnectionStrings__POSTGRES_DEMO = $connectionString

    Write-Host ""
    Write-Host "Starting SearchEngine.Service with PostgreSQL demo source"
    Write-Host "URL: $Url"
    Write-Host "Environment: PostgresDemo"
    Write-Host "Data source: postgres-demo"
    Write-Host ""
    Write-Host "Check source:"
    Write-Host "GET  $Url/v1/data-sources"
    Write-Host ""
    Write-Host "Build index from source:"
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

    Write-Host ""
    Write-Host "Stopping PostgreSQL demo container"

    try {
        Remove-DemoContainerIfExists
    }
    catch {
        Write-Host "PostgreSQL demo container cleanup skipped."
    }
}