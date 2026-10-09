# ClassicFX RenderCircle V7. Requires compiled V6 (MagicGround2).
# Designed for Windows PowerShell 5.1+, with exact-anchor preflight and rollback.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$effectsPath = Join-Path $root 'Client.Main\ClassicFX\Core\ClassicFxRuntime.Effects.cs'
$texturesPath = Join-Path $root 'Client.Main\ClassicFX\Data\ClassicTextureRepository.cs'
$v6Path = Join-Path $root 'Client.Main\ClassicFX\Core\ClassicFxRuntime.EffectMagicGround2.V6.cs'
$targetPath = Join-Path $root 'Client.Main\ClassicFX\Core\ClassicFxRuntime.EffectRenderCircle.V7.cs'
$payloadPath = Join-Path $root 'payload\ClassicFxRuntime.EffectRenderCircle.V7.cs'
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Normalize-LF([string]$value) { return $value.Replace("`r`n", "`n") }
function Replace-Unique([string]$source, [string]$from, [string]$to, [string]$label) {
    $count = ([regex]::Matches($source, [regex]::Escape($from))).Count
    if ($count -ne 1) {
        throw "Anchor '$label' expected once, found $count. Repo differs; no files written."
    }
    return $source.Replace($from, $to)
}
foreach ($file in @($effectsPath, $texturesPath, $v6Path, $payloadPath)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Required file missing: $file. Install and compile V6 first."
    }
}
$sourceEffects = [IO.File]::ReadAllText($effectsPath)
$sourceTextures = [IO.File]::ReadAllText($texturesPath)
$effectCode = Normalize-LF $sourceEffects
$textureCode = Normalize-LF $sourceTextures
$payload = Normalize-LF ([IO.File]::ReadAllText($payloadPath))
$v6Code = Normalize-LF ([IO.File]::ReadAllText($v6Path))
if (-not ($v6Code.Contains('private static bool TryGetMagicGround2Definition(') -and
           $v6Code.Contains('private float InitializeMagicGround2Scale(') -and
           $effectCode.Contains('MagicGround2 = 21') -and
           $effectCode.Contains('RenderMagicGround2(ref e);') -and
           $effectCode.Contains('LastChildNativeTick = -1,'))) {
    throw 'V6 installation is missing/incomplete. No files written.'
}
$hasEnum = $effectCode.Contains('MagicCircleGround = 22')
$hasTexture = $textureCode.Contains('ClassicTextureIds.BitmapMagic + 2,')
$hasFile = Test-Path -LiteralPath $targetPath -PathType Leaf
if ($hasEnum -or $hasTexture -or $hasFile) {
    if (-not ($hasEnum -and $hasTexture -and $hasFile -and
        $effectCode.Contains('RenderMagicCircleGround(ref e);') -and
        $effectCode.Contains('bool magicCircleGround =') -and
        $effectCode.Contains('e.Type == ClassicFxEffectType.MagicCircleGround'))) {
        throw 'Partial/different V7 installation found. No changes made; inspect git diff.'
    }
    if ((Normalize-LF ([IO.File]::ReadAllText($targetPath))) -ne $payload) {
        throw 'Existing V7 code differs from installer payload. Refusing overwrite.'
    }
    Write-Host 'ClassicFX RenderCircle V7 already installed. No files changed.' -ForegroundColor Green
    exit 0
}

# Construct all replacements in memory. Fail on any changed anchor before writing.
$old = @'
        MagicGround2 = 21
'@
$new = @'
        MagicGround2 = 21,
        MagicCircleGround = 22
'@
$effectCode = Replace-Unique $effectCode $old $new 'Effect enum'

$old = @'
            MagicGround2Definition magicGround2Definition = default;
            bool magicGround2 = type == ClassicFxEffectType.MagicGround2 &&
                TryGetMagicGround2Definition(subType, out magicGround2Definition);
'@
$new = @'
            MagicGround2Definition magicGround2Definition = default;
            bool magicGround2 = type == ClassicFxEffectType.MagicGround2 &&
                TryGetMagicGround2Definition(subType, out magicGround2Definition);
            // Both BITMAP_MAGIC+1 and BITMAP_MAGIC+2 share native CreateEffect().
            MagicGround2Definition magicCircleDefinition = default;
            bool magicCircleGround = type == ClassicFxEffectType.MagicCircleGround &&
                TryGetMagicGround2Definition(subType, out magicCircleDefinition);
'@
$effectCode = Replace-Unique $effectCode $old $new 'V7 subtype definition'

$old = @'
            if (magicGround2 || additionalTerrain)
'@
$new = @'
            if (magicGround2 || magicCircleGround || additionalTerrain)
'@
$effectCode = Replace-Unique $effectCode $old $new 'owner/world eligibility'

$old = @'
            else if (magicGround2)
            {
                life = magicGround2Definition.LifeTime;
                effectScale = InitializeMagicGround2Scale(
                    in magicGround2Definition, scale);
                if (magicGround2Definition.RandomAngle)
                    angle.Z = Random.Modulo(360); // degrees: native BITMAP_MAGIC+1:7
            }
            else if (terrain)
'@
$new = @'
            else if (magicGround2)
            {
                life = magicGround2Definition.LifeTime;
                effectScale = InitializeMagicGround2Scale(
                    in magicGround2Definition, scale);
                if (magicGround2Definition.RandomAngle)
                    angle.Z = Random.Modulo(360); // degrees: native BITMAP_MAGIC+1:7
            }
            else if (magicCircleGround)
            {
                // Original CreateEffect(BITMAP_MAGIC+2) shares init with +1.
                life = magicCircleDefinition.LifeTime;
                effectScale = InitializeMagicGround2Scale(
                    in magicCircleDefinition, scale);
                if (magicCircleDefinition.RandomAngle)
                    angle.Z = Random.Modulo(360);
            }
            else if (terrain)
'@
$effectCode = Replace-Unique $effectCode $old $new 'native Effect init'

$old = @'
                else if (e.Type == ClassicFxEffectType.MagicGround2)
                {
                    MoveMagicGround2(ref e, f);
                }
                else if (terrain)
'@
$new = @'
                else if (e.Type == ClassicFxEffectType.MagicGround2)
                {
                    MoveMagicGround2(ref e, f);
                }
                else if (e.Type == ClassicFxEffectType.MagicCircleGround)
                {
                    // MuMain +2 has no Move handler: the shared pool ages it.
                }
                else if (terrain)
'@
$effectCode = Replace-Unique $effectCode $old $new 'MoveEffects dispatch'

$old = @'
                if (e.Type == ClassicFxEffectType.MagicGround2)
                {
                    RenderMagicGround2(ref e);
                    continue;
                }
                if (IsV5TerrainEffectType(e.Type))
'@
$new = @'
                if (e.Type == ClassicFxEffectType.MagicGround2)
                {
                    RenderMagicGround2(ref e);
                    continue;
                }
                if (e.Type == ClassicFxEffectType.MagicCircleGround)
                {
                    RenderMagicCircleGround(ref e);
                    continue;
                }
                if (IsV5TerrainEffectType(e.Type))
'@
$effectCode = Replace-Unique $effectCode $old $new 'RenderEffects dispatch'

$old = @'
                new(
                    ClassicTextureIds.BitmapMagic + 1,
                    "Effect/Magic_Ground2.jpg",
                    SamplerState.LinearClamp),
'@
$new = @'
                new(
                    ClassicTextureIds.BitmapMagic + 1,
                    "Effect/Magic_Ground2.jpg",
                    SamplerState.LinearClamp),

                // MuMain ZzzOpenData.cpp: BITMAP_MAGIC+2 = Magic_Circle1.
                // Data_Broyal physical asset: Effect/Magic_Circle1.OZJ.
                new(
                    ClassicTextureIds.BitmapMagic + 2,
                    "Effect/Magic_Circle1.jpg",
                    SamplerState.LinearWrap),
'@
$textureCode = Replace-Unique $textureCode $old $new 'MagicCircle texture registration'

$nlFx = if ($sourceEffects.Contains("`r`n")) { "`r`n" } else { "`n" }
$nlTx = if ($sourceTextures.Contains("`r`n")) { "`r`n" } else { "`n" }
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss_fff'
$backupDir = Join-Path ([IO.Path]::GetTempPath()) ('ClassicFX_RenderCircle_V7_' + $stamp)
[IO.Directory]::CreateDirectory($backupDir) | Out-Null
[IO.File]::WriteAllText((Join-Path $backupDir 'ClassicFxRuntime.Effects.cs'), $sourceEffects, $utf8)
[IO.File]::WriteAllText((Join-Path $backupDir 'ClassicTextureRepository.cs'), $sourceTextures, $utf8)
try {
    [IO.File]::WriteAllText($effectsPath, $effectCode.Replace("`n", $nlFx), $utf8)
    [IO.File]::WriteAllText($texturesPath, $textureCode.Replace("`n", $nlTx), $utf8)
    [IO.File]::WriteAllText($targetPath, $payload.Replace("`n", $nlFx), $utf8)
} catch {
    [IO.File]::WriteAllText($effectsPath, $sourceEffects, $utf8)
    [IO.File]::WriteAllText($texturesPath, $sourceTextures, $utf8)
    Remove-Item -LiteralPath $targetPath -Force -ErrorAction SilentlyContinue
    throw
}
Write-Host "ClassicFX RenderCircle V7 installed. Backup: $backupDir" -ForegroundColor Green
Write-Host 'Next: dotnet build .\Client.Main\Client.Main.csproj ; git diff --check'
