# BroyalMU - One-branch consolidation, safe for Windows PowerShell 5.1+.
# Prerequisites: git push access and GitHub CLI (gh) authenticated with admin rights.
# Preview: powershell -ExecutionPolicy Bypass -File .\tools\consolidate-classicfx-branches.ps1 -DryRun
# Execute: powershell -ExecutionPolicy Bypass -File .\tools\consolidate-classicfx-branches.ps1
# Non-ancestor branches are backed up as remote tags before deletion.
[CmdletBinding()]
param([switch]$DryRun)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = 'Ignacioctoro/muonline-client'
$Keep = 'classicfx-nova-pilot'
$TagPrefix = 'archive/classicfx-consolidation-20261011'

function Assert-Exit([string]$Operation) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Operation falló (código $LASTEXITCODE). No se continuó."
    }
}

function Git-Checked([string[]]$Arguments) {
    & git @Arguments
    Assert-Exit ("git " + ($Arguments -join ' '))
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'No se encontró Git.'
}

# Run from the repository root, regardless of the caller working directory.
Set-Location (Resolve-Path (Join-Path $PSScriptRoot '..'))
$top = (& git rev-parse --show-toplevel).Trim()
Assert-Exit 'git rev-parse'
$origin = (& git remote get-url origin).Trim()
Assert-Exit 'git remote get-url origin'

if ($origin -notmatch '(?i)github\.com[:/]Ignacioctoro/muonline-client(?:\.git)?/?$') {
    throw "Origin inesperado: $origin. Se evita borrar ramas de otro repositorio."
}

Git-Checked @('fetch', 'origin', '--prune', '--tags')
$headLines = @(& git ls-remote --heads origin)
Assert-Exit 'git ls-remote --heads origin'

$heads = @{}
foreach ($line in $headLines) {
    if ($line -match '^([0-9a-f]{40})\s+refs/heads/(.+)$') {
        $heads[$Matches[2]] = $Matches[1]
    }
}
if (-not $heads.ContainsKey($Keep)) {
    throw "No existe la rama remota $Keep."
}

$originalPilotSha = $heads[$Keep]
$others = @($heads.Keys | Where-Object { $_ -ne $Keep } | Sort-Object)
$divergent = @()

foreach ($name in $others) {
    $ref = 'refs/remotes/origin/' + $name
    & git merge-base --is-ancestor $ref ('refs/remotes/origin/' + $Keep)
    $mergeStatus = $LASTEXITCODE

    if ($mergeStatus -eq 1) {
        $divergent += [pscustomobject]@{
            Branch = $name
            Sha = $heads[$name]
            Tag = "$TagPrefix/$name"
        }
    }
    elseif ($mergeStatus -ne 0) {
        throw "No se pudo comparar $name con $Keep. Se detuvo por seguridad."
    }
}

Write-Host "Repositorio: $Repo"
Write-Host "Rama que se conserva: $Keep ($originalPilotSha)"
Write-Host "Ramas candidatas para borrar: $($others.Count)"
Write-Host "Ramas NO ancestras que requieren tag de respaldo: $($divergent.Count)"
foreach ($item in $divergent) {
    Write-Host "  $($item.Branch) => $($item.Tag) ($($item.Sha))"
}

if ($DryRun) {
    Write-Host 'MODO SIMULACIÓN: ningún tag, rama ni configuración fue modificado.'
    return
}

# Preserve every commit tip that is not reachable from the retained branch.
# Retains historical code without merging obsolete files into Batch 45.
foreach ($item in $divergent) {
    $tagRef = 'refs/tags/' + $item.Tag
    $localTarget = & git rev-parse -q --verify $tagRef
    $localResult = $LASTEXITCODE

    if ($localResult -eq 0) {
        if ($localTarget.Trim() -ne $item.Sha) {
            throw "El tag $($item.Tag) ya existe pero apunta a otro commit."
        }
    }
    elseif ($localResult -eq 1) {
        Git-Checked @('tag', $item.Tag, $item.Sha)
    }
    else {
        throw "No se pudo consultar el tag $($item.Tag)."
    }

    Git-Checked @('push', 'origin', ($tagRef + ':' + $tagRef))
}

# Only GitHub repository administration can change default_branch.
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw @'
Falta GitHub CLI (gh). Los tags de respaldo ya se guardaron.
Instálalo con: winget install --id GitHub.cli
Inicia sesión con: gh auth login
Después ejecuta este mismo script nuevamente.
No se borró ninguna rama.
'@
}

$default = (& gh repo view $Repo --json defaultBranchRef --jq '.defaultBranchRef.name').Trim()
Assert-Exit 'gh repo view'
if ($default -ne $Keep) {
    & gh repo edit $Repo --default-branch $Keep
    Assert-Exit 'gh repo edit --default-branch'
}
$default = (& gh repo view $Repo --json defaultBranchRef --jq '.defaultBranchRef.name').Trim()
Assert-Exit 'gh repo view (verificación)'
if ($default -ne $Keep) {
    throw "La rama predeterminada todavía es $default; no se borró ninguna rama."
}

# Refuse to delete if any branch changed since the snapshot.
$verificationLines = @(& git ls-remote --heads origin)
Assert-Exit 'git ls-remote (verificación)'
$verifyHeads = @{}
foreach ($line in $verificationLines) {
    if ($line -match '^([0-9a-f]{40})\s+refs/heads/(.+)$') {
        $verifyHeads[$Matches[2]] = $Matches[1]
    }
}
if ($verifyHeads.Count -ne $heads.Count) {
    throw 'El número de ramas cambió durante la operación. Reejecuta el script.'
}
foreach ($name in $heads.Keys) {
    if (-not $verifyHeads.ContainsKey($name) -or $verifyHeads[$name] -ne $heads[$name]) {
        throw "La rama $name cambió de commit. Reejecuta el script."
    }
}

foreach ($name in $others) {
    Git-Checked @('push', 'origin', '--delete', $name)
}
Git-Checked @('fetch', 'origin', '--prune')

$lastHeads = @(& git ls-remote --heads origin)
Assert-Exit 'git ls-remote (final)'
$expected = $originalPilotSha + [char]9 + 'refs/heads/' + $Keep
if ($lastHeads.Count -ne 1 -or $lastHeads[0] -ne $expected) {
    throw 'Limpieza parcial: comprueba las ramas restantes en GitHub.'
}

Write-Host "LISTO: solo existe la rama $Keep ($originalPilotSha)."
Write-Host "Se conservaron $($divergent.Count) tags de archivo para las ramas divergentes."
