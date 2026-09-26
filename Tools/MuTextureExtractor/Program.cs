using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using Client.Data.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MuTextureExtractor;

internal static class Program
{
    private static readonly string[] SupportedExtensions =
    [
        ".ozd",
        ".ozt",
        ".ozj"
    ];

    private static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            PrintUsage();
            return 1;
        }

        string inputPath = Path.GetFullPath(args[0]);

        string outputPath =
            args.Length >= 2 &&
            !args[1].StartsWith("--", StringComparison.Ordinal)
                ? Path.GetFullPath(args[1])
                : Path.Combine(inputPath, "_MU_Texture_Preview");

        bool overwrite = args.Any(
            a => string.Equals(
                a,
                "--overwrite",
                StringComparison.OrdinalIgnoreCase));

        if (!Directory.Exists(inputPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No existe la carpeta de entrada:");
            Console.WriteLine(inputPath);
            Console.ResetColor();
            return 2;
        }

        Directory.CreateDirectory(outputPath);

        string[] files = Directory
            .EnumerateFiles(inputPath, "*.*", SearchOption.AllDirectories)
            .Where(path =>
                SupportedExtensions.Contains(
                    Path.GetExtension(path),
                    StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Console.WriteLine();
        Console.WriteLine("MU Texture Extractor");
        Console.WriteLine("--------------------");
        Console.WriteLine($"Entrada : {inputPath}");
        Console.WriteLine($"Salida  : {outputPath}");
        Console.WriteLine($"Archivos: {files.Length}");
        Console.WriteLine($"Modo    : {(overwrite ? "sobrescribir" : "omitir existentes")}");
        Console.WriteLine();

        if (files.Length == 0)
        {
            Console.WriteLine("No se encontraron archivos .OZD, .OZT o .OZJ.");
            return 0;
        }

        var dxtDecoder = new BcDecoder();

        int converted = 0;
        int skipped = 0;
        int failed = 0;

        for (int i = 0; i < files.Length; i++)
        {
            string source = files[i];

            string relative =
                Path.GetRelativePath(inputPath, source);

            string relativePng =
                Path.ChangeExtension(relative, ".png");

            string destination =
                Path.Combine(outputPath, relativePng);

            Console.Write(
                $"[{i + 1,5}/{files.Length}] {relative} ... ");

            try
            {
                if (!overwrite &&
                    File.Exists(destination))
                {
                    skipped++;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("omitido");
                    Console.ResetColor();
                    continue;
                }

                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination)!);

                string extension =
                    Path.GetExtension(source).ToLowerInvariant();

                TextureData texture =
                    extension switch
                    {
                        ".ozd" => await new OZDReader().Load(source),
                        ".ozt" => await new OZTReader().Load(source),
                        ".ozj" => await new OZJReader().Load(source),
                        _ => throw new NotSupportedException(
                            $"Extensión no soportada: {extension}")
                    };

                using Image image =
                    CreateImage(texture, dxtDecoder);

                await image.SaveAsPngAsync(destination);

                converted++;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"OK ({texture.Width}x{texture.Height}, {extension.ToUpperInvariant()})");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                failed++;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(
                    $"        {ex.GetType().Name}: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Console.WriteLine("Resultado");
        Console.WriteLine("---------");
        Console.WriteLine($"Convertidos : {converted}");
        Console.WriteLine($"Omitidos    : {skipped}");
        Console.WriteLine($"Errores     : {failed}");
        Console.WriteLine($"Salida      : {outputPath}");

        return failed == 0 ? 0 : 3;
    }

    private static Image CreateImage(
        TextureData texture,
        BcDecoder dxtDecoder)
    {
        if (texture.IsCompressed)
        {
            CompressionFormat format =
                texture.Format switch
                {
                    TextureSurfaceFormat.Dxt1 => CompressionFormat.Bc1,
                    TextureSurfaceFormat.Dxt3 => CompressionFormat.Bc2,
                    TextureSurfaceFormat.Dxt5 => CompressionFormat.Bc3,
                    _ => throw new NotSupportedException(
                        $"Formato comprimido no soportado: {texture.Format}")
                };

            return dxtDecoder.DecodeRawToImageRgba32(
                texture.Data,
                texture.Width,
                texture.Height,
                format);
        }

        return texture.Components switch
        {
            4 => Image.LoadPixelData<Rgba32>(
                texture.Data,
                texture.Width,
                texture.Height),

            3 => Image.LoadPixelData<Rgb24>(
                texture.Data,
                texture.Width,
                texture.Height),

            _ => throw new NotSupportedException(
                $"Cantidad de componentes no soportada: {texture.Components}")
        };
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            MU Texture Extractor

            Convierte:
              .OZD -> PNG
              .OZT -> PNG
              .OZJ -> PNG

            Uso:
              dotnet run --project Tools\MuTextureExtractor -- "<entrada>" ["<salida>"] [--overwrite]

            Ejemplos:

              dotnet run --project Tools\MuTextureExtractor -- "G:\BRoyalMu\Data\Interface"

              dotnet run --project Tools\MuTextureExtractor -- "G:\BRoyalMu\Data\Interface" "G:\BRoyalMu\TexturePreview"

              dotnet run --project Tools\MuTextureExtractor -- "G:\BRoyalMu\Data\Interface" "G:\BRoyalMu\TexturePreview" --overwrite
            """);
    }
}
