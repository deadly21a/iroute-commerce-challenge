param([string]$Pnpm = 'pnpm')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
Push-Location -LiteralPath "$taskRoot/frontend"
try { & $Pnpm start }
finally { Pop-Location }
