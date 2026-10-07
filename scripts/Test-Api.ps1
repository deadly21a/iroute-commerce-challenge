param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskPassed = 0
function Assert-Equal($Actual, $Expected, [string]$Label) {
    if ($Actual -ne $Expected) { throw "$Label`: esperado $Expected, obtenido $Actual" }
    $script:taskPassed++
    Write-Output "PASS $Label"
}
function Send-Json([string]$Path, $Body, $Headers = @{}) {
    Invoke-RestMethod -Method Post -Uri "$BaseUrl$Path" -Headers $Headers -ContentType 'application/json' -Body ($Body | ConvertTo-Json)
}
$taskHealth = Invoke-RestMethod "$BaseUrl/api/health"
Assert-Equal $taskHealth.status 'healthy' 'SQL Server disponible'
$taskLogin = Send-Json '/api/auth/login' @{email='demo@iroute.local';password='Comercio2026!'}
$taskHeaders = @{Authorization="Bearer $($taskLogin.token)"}
Assert-Equal ([string]::IsNullOrEmpty($taskLogin.token)) $false 'Login devuelve token'
# Fecha reservada y contenido único para que cada ejecución sea independiente.
$taskSuffix = [guid]::NewGuid().ToString('N')
$taskCsv = "pc_processdate,pc_nomcomred,pc_numdoc,pc_email,pc_telefono,pc_direccion`n2099-12-30,Tienda $taskSuffix,000123,,,`n2099-12-30,,AB-12,,,`n2099-12-31,Otro $taskSuffix,987,,,`n"
$taskTemp = Join-Path ([IO.Path]::GetTempPath()) ("iroute-smoke-" + $taskSuffix)
New-Item -ItemType Directory -Path $taskTemp | Out-Null
$taskFile = Join-Path $taskTemp 'commerce_30122099.csv'
[IO.File]::WriteAllText($taskFile, $taskCsv, [Text.UTF8Encoding]::new($false))
try {
    # Requiere PowerShell 7 para multipart con -Form.
    $taskImport = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/commerce/import" -Headers $taskHeaders -Form @{file=Get-Item -LiteralPath $taskFile}
    Assert-Equal $taskImport.insertedCount 3 'Importación completa'
    try {
        Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/commerce/import" -Headers $taskHeaders -Form @{file=Get-Item -LiteralPath $taskFile} | Out-Null
        throw 'No rechazó archivo duplicado'
    } catch {
        if (-not $_.Exception.Response) { throw }
        Assert-Equal ([int]$_.Exception.Response.StatusCode) 409 'Duplicado rechazado'
    }
    $taskProcessed = Send-Json '/api/commerce/process' @{processDate='2099-12-30'} $taskHeaders
    Assert-Equal $taskProcessed.quarantinedCount 1 'Un registro nuevo a cuarentena'
    $taskRepeated = Send-Json '/api/commerce/process' @{processDate='2099-12-30'} $taskHeaders
    Assert-Equal $taskRepeated.quarantinedCount 0 'Reprocesamiento idempotente'
    $taskQuarantine = Invoke-RestMethod -Uri "$BaseUrl/api/commerce/quarantine?processDate=2099-12-30&page=1&pageSize=100" -Headers $taskHeaders
    $taskItem = $taskQuarantine.items | Where-Object { $_.batchId -eq $taskImport.batchId }
    Assert-Equal @($taskItem).Count 1 'Consulta conserva lote'
    Assert-Equal ($taskItem.motivo -match 'nombre.*documento') $true 'Ambos motivos registrados'
    try { Invoke-RestMethod -Uri "$BaseUrl/api/commerce/overview" | Out-Null; throw 'Endpoint sin proteger' }
    catch {
        if (-not $_.Exception.Response) { throw }
        Assert-Equal ([int]$_.Exception.Response.StatusCode) 401 'API protegida'
    }
    Write-Output "Verificación terminada: $taskPassed comprobaciones correctas. Datos de prueba conservados en las fechas 2099-12-30 y 2099-12-31."
} finally { Remove-Item -LiteralPath $taskFile -Force; Remove-Item -LiteralPath $taskTemp -Force }
