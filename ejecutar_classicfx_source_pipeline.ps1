param(
    [string]$Client = (Get-Location).Path,
    [switch]$Offline
)

$ErrorActionPreference = 'Stop'
$clientPath = (Resolve-Path -LiteralPath $Client).Path
$scriptPath = Join-Path $PSScriptRoot 'classicfx_source_pipeline.py'
if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "No encuentro classicfx_source_pipeline.py en $PSScriptRoot"
}
if (-not (Test-Path -LiteralPath (Join-Path $clientPath 'Client.Main\ClassicFX\Core\ClassicFxRuntime.Effects.cs'))) {
    throw "No encuentro ClassicFX en $clientPath. Ejecute desde C:\muonline-client-main o pase -Client"
}
$python = Get-Command py -ErrorAction SilentlyContinue
if ($python) {
    $exe = 'py'
    $pythonArguments = @('-3')
} else {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if (-not $python) { throw 'Se requiere Python 3.9 o posterior (python o py -3).' }
    $exe = 'python'
    $pythonArguments = @()
}
$pythonArguments += @($scriptPath, '--client', $clientPath)
if ($Offline) { $pythonArguments += '--offline' }
Write-Host "Analizando fuentes MuMain sin modificar el cliente: $clientPath"
& $exe @pythonArguments
if ($LASTEXITCODE -ne 0) { throw "El analizador termino con error $LASTEXITCODE" }
Write-Host "Listo: $clientPath\classicfx_pipeline_fuente"
