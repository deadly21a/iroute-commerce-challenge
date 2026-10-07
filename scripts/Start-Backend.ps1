param([string]$ConnectionString, [string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$env:ASPNETCORE_ENVIRONMENT = 'Development'
if ($ConnectionString) { $env:ConnectionStrings__Commerce = $ConnectionString }
& $Dotnet run --project "$taskRoot/backend/Commerce.Api" --urls http://localhost:5080
