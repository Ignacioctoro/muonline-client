# MU Texture Extractor

Convierte recursivamente:

```text
.OZD -> PNG
.OZT -> PNG
.OZJ -> PNG
```

## Instalación

Copia esta carpeta a:

```text
C:\muonline-client-main\Tools\MuTextureExtractor
```

## Uso con BRoyalMu

```powershell
cd C:\muonline-client-main

dotnet run --project Tools\MuTextureExtractor -- "G:\BRoyalMu\Data\Interface" "G:\BRoyalMu\TexturePreview"
```

## Sobrescribir previews

```powershell
dotnet run --project Tools\MuTextureExtractor -- "G:\BRoyalMu\Data\Interface" "G:\BRoyalMu\TexturePreview" --overwrite
```

## Buscar candidatos al HUD

```powershell
Get-ChildItem "G:\BRoyalMu\TexturePreview" -Recurse -File |
Where-Object {
    $_.Name -match "position|map|helper|macro|main|hud|mini|coord"
} |
Select-Object FullName
```
