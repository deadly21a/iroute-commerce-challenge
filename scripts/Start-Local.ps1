param([string]$ConnectionString, [string]$Dotnet, [string]$Pnpm)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskLocal = Join-Path $taskRoot '.local'
New-Item -ItemType Directory -Path $taskLocal -Force | Out-Null
$taskConfigFile = Join-Path $taskLocal 'tools.json'
$taskConfig = if (Test-Path -LiteralPath $taskConfigFile) { Get-Content -LiteralPath $taskConfigFile -Raw | ConvertFrom-Json } else { $null }
if (-not $Dotnet) { $Dotnet = if ($taskConfig) { $taskConfig.Dotnet } else { 'dotnet' } }
if (-not $Pnpm) { $Pnpm = if ($taskConfig) { $taskConfig.Pnpm } else { 'pnpm' } }
if (-not $ConnectionString) { $ConnectionString = if ($taskConfig) { $taskConfig.ConnectionString } else { 'Server=(localdb)\MSSQLLocalDB;Database=IRouteCommerce;Integrated Security=true;Encrypt=true;TrustServerCertificate=true' } }
$taskShell = (Get-Process -Id $PID).Path
function Test-Port([int]$Port) {
    $taskClient = [Net.Sockets.TcpClient]::new()
    try { return $taskClient.ConnectAsync('localhost', $Port).Wait(500) -and $taskClient.Connected }
    catch { return $false }
    finally { $taskClient.Dispose() }
}
if (-not (Test-Port 5080)) {
    & "$PSScriptRoot/Initialize-Database.ps1" -ConnectionString $ConnectionString -Dotnet $Dotnet
    Start-Process -FilePath $taskShell -ArgumentList @('-NoProfile','-File', ('"' + $PSScriptRoot + '\Start-Backend.ps1"'), '-ConnectionString', ('"' + $ConnectionString + '"'), '-Dotnet', ('"' + $Dotnet + '"')) -WorkingDirectory $taskRoot -WindowStyle Hidden -RedirectStandardOutput "$taskLocal/backend.log" -RedirectStandardError "$taskLocal/backend-error.log" | Out-Null
}
if (-not (Test-Port 4200)) {
    Start-Process -FilePath $taskShell -ArgumentList @('-NoProfile','-File', ('"' + $PSScriptRoot + '\Start-Frontend.ps1"'), '-Pnpm', ('"' + $Pnpm + '"')) -WorkingDirectory $taskRoot -WindowStyle Hidden -RedirectStandardOutput "$taskLocal/frontend.log" -RedirectStandardError "$taskLocal/frontend-error.log" | Out-Null
}
Write-Output 'Aplicación: http://localhost:4200'
Write-Output 'Swagger: http://localhost:5080/swagger'
Write-Output 'Demo: demo@iroute.local / Comercio2026!'
Write-Output 'El primer arranque puede tardar unos segundos. Logs en .local si aparece un error.'
