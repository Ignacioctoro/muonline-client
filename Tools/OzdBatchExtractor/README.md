# OZD Batch Extractor

Convierte de forma masiva los `.ozd` de MU Online a `.png` reutilizando el mismo
`OZDReader` / `ModulusCryptor` del cliente NeffisDev.

## Dónde ponerlo

Copia esta carpeta completa a:

```text
C:\muonline-client-main\Tools\OzdBatchExtractor
```

La estructura debe quedar:

```text
C:\muonline-client-main
├── Client.Data
├── Client.Main
├── Tools
│   └── OzdBatchExtractor
│       ├── OzdBatchExtractor.csproj
│       └── Program.cs
└── ...
```

## Convertir todos los OZD de Interface

Desde:

```powershell
cd C:\muonline-client-main
```

ejecuta:

```powershell
dotnet run --project Tools\OzdBatchExtractor -- "C:\Data_Broyal\Data\Interface" "C:\Data_Broyal\OZD_Preview"
```

El programa:

- busca `.ozd` recursivamente;
- mantiene la estructura de carpetas;
- descifra OZD usando `Client.Data.Texture.OZDReader`;
- decodifica DXT1/DXT3/DXT5;
- guarda PNG;
- omite PNG ya existentes.

Ejemplo de salida:

```text
C:\Data_Broyal\OZD_Preview
├── newui_position02.png
├── Minimap_positionA.png
└── GFx
    ├── hudMap_I4.png
    ├── MacroMain_I1.png
    └── ...
```

## Sobrescribir previews existentes

Agrega:

```text
--overwrite
```

Ejemplo:

```powershell
dotnet run --project Tools\OzdBatchExtractor -- "C:\Data_Broyal\Data\Interface" "C:\Data_Broyal\OZD_Preview" --overwrite
```

## Solo GFx

Para la búsqueda del HUD de mapa puede ser más práctico empezar solo por GFx:

```powershell
dotnet run --project Tools\OzdBatchExtractor -- "C:\Data_Broyal\Data\Interface\GFx" "C:\Data_Broyal\OZD_Preview_GFx"
```
