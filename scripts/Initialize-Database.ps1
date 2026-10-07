param([string]$ConnectionString, [string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$env:ASPNETCORE_ENVIRONMENT = 'Development'
if ($ConnectionString) { $env:ConnectionStrings__Commerce = $ConnectionString }
& $Dotnet run --project "$taskRoot/backend/Commerce.Api" -- --initialize-db
if ($LASTEXITCODE -ne 0) { throw 'No se pudo inicializar SQL Server. Revisa la instancia y los permisos.' }
