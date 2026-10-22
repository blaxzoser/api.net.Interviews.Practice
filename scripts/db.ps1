<#
.SYNOPSIS
  Starts the SQL Server container and creates + seeds TeeTimeDb (first run only).
.PARAMETER Reset
  Drops TeeTimeDb first, so schema and seed are rebuilt from scratch.
#>
param([switch]$Reset)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

if (-not (Test-Path '.env')) {
    Copy-Item '.env.example' '.env'
    Write-Host "Created .env from .env.example. Edit MSSQL_SA_PASSWORD if you want, then run this script again."
    exit 1
}

# SQLCMDPASSWORD is set inside the container (see compose.yaml), so no password appears on any command line.
$sqlcmd = '/opt/mssql-tools18/bin/sqlcmd'

function Invoke-SqlFile([string]$File, [string]$Database = 'master') {
    docker compose exec -T sqlserver $sqlcmd -C -I -b -S localhost -U sa -d $Database -i "/sql/$File"
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed on $File" }
}

Write-Host "Starting SQL Server (waits until healthy)..."
docker compose up -d --wait
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }

if ($Reset) {
    Write-Host "Dropping TeeTimeDb..."
    Invoke-SqlFile '99-drop-database.sql'
}

$dbId = (docker compose exec -T sqlserver $sqlcmd -C -h -1 -W -S localhost -U sa -Q "SET NOCOUNT ON; SELECT ISNULL(DB_ID('TeeTimeDb'), 0)" | Out-String).Trim()

if ($dbId -eq '0') {
    Write-Host "Creating database, schema and seed data (about a minute)..."
    Invoke-SqlFile '00-create-database.sql'
    Invoke-SqlFile '01-schema.sql' 'TeeTimeDb'
    Invoke-SqlFile '02-seed.sql'   'TeeTimeDb'
    Write-Host "Done."
}
else {
    Write-Host "TeeTimeDb already exists. Use -Reset to rebuild it."
}
