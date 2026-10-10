# BroyalMU ClassicFX Season 6 - integra Batch 09 + Batch 10 en un solo fast-forward.
# NO resetea, NO borra archivos, NO modifica tests/, NO fuerza merge.
$ErrorActionPreference = 'Stop'
$repoPath = 'C:\muonline-client-main'
$expectedBase = '3b3a7285957be28a5172b9ee89505a68ece9c7b3'
$expectedBatch10 = '0e0d344e6eb492fb945be6b124948d635b0b5024'
$remoteBranch = 'classicfx-s6-batch10-20261010'

function Invoke-GitChecked([string[]]$Arguments) {
    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git falló: git $($Arguments -join ' ') (exit $LASTEXITCODE)"
    }
}

if (-not (Test-Path -LiteralPath $repoPath -PathType Container)) {
    throw "No existe el repositorio: $repoPath"
}
Set-Location -LiteralPath $repoPath
$inside = (& git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or -not $inside) { throw 'No es un repositorio Git.' }
$localBranch = (& git branch --show-current).Trim()
if ($localBranch -ne 'classicfx-nova-pilot') {
    throw "Rama inesperada '$localBranch'. Cambia a classicfx-nova-pilot ANTES de ejecutar, sin perder cambios."
}
$status = @(& git status --porcelain)
if ($LASTEXITCODE -ne 0) { throw 'git status falló.' }
if (@($status | Where-Object { $_ -and $_.Trim() }).Count -gt 0) {
    throw "Worktree contiene modificaciones o archivos sin seguimiento. NO se tocó ningún archivo.`n$($status -join "`n")"
}
$head = (& git rev-parse HEAD).Trim()
if ($head -ne $expectedBase) {
    throw "HEAD distinto al corte verificado ($head). NO se aplicó nada."
}
$originUrl = (& git remote get-url origin).Trim()
if ($LASTEXITCODE -ne 0 -or $originUrl -notmatch '(?i)Ignacioctoro[/\\]muonline-client(?:\.git)?$') {
    throw "Origen GitHub inesperado: $originUrl. NO se aplicó nada."
}

Invoke-GitChecked -Arguments @('fetch','--no-tags','origin',"refs/heads/$remoteBranch")
$fetched = (& git rev-parse FETCH_HEAD).Trim()
if ($fetched -ne $expectedBatch10) {
    throw "Batch10 remoto cambió ($fetched). NO se aplicó nada; revisar el commit antes de integrar."
}
& git merge-base --is-ancestor HEAD FETCH_HEAD
if ($LASTEXITCODE -ne 0) { throw 'El commit remoto no desciende de HEAD. NO se aplicó nada.' }
Invoke-GitChecked -Arguments @('merge','--ff-only','FETCH_HEAD')

Write-Host 'Batch 09 y 10 integrados por fast-forward. Compilando una sola vez...' -ForegroundColor Cyan
& dotnet build .\Client.Main\Client.Main.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Warning 'Compilación fallida: conserva todo tal como quedó; envía el log completo para corregir errores en conjunto.'
    exit $LASTEXITCODE
}
Invoke-GitChecked -Arguments @('diff','--check',"$expectedBase..HEAD")
Write-Host 'Compilación OK, diff --check OK. Estado actual:' -ForegroundColor Green
Invoke-GitChecked -Arguments @('log','-3','--oneline')
Invoke-GitChecked -Arguments @('status','--short')
Write-Host 'El push a origin queda bajo tu control: git push origin classicfx-nova-pilot' -ForegroundColor Yellow
