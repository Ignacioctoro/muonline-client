using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using Client.Data.Texture;
using SixLabors.ImageSharp;

namespace OzdBatchExtractor;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            PrintUsage();
            return 1;
        }

        string inputPath = Path.GetFullPath(args[0]);

        string outputPath =
            args.Length >= 2 && !args[1].StartsWith("--", StringComparison.Ordinal)
                ? Path.GetFullPath(args[1])
                : Path.Combine(inputPath, "_OZD_Preview");

        bool overwrite = args.Any(
            a => string.Equals(a, "--overwrite", StringComparison.OrdinalIgnoreCase));

        if (!Directory.Exists(inputPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"No existe la carpeta de entrada:");
            Console.WriteLine(inputPath);
            Console.ResetColor();
            return 2;
        }

        Directory.CreateDirectory(outputPath);

        string[] files = Directory
            .EnumerateFiles(inputPath, "*.ozd", SearchOption.AllDirectories)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Console.WriteLine();
        Console.WriteLine("OZD Batch Extractor");
        Console.WriteLine("-------------------");
        Console.WriteLine($"Entrada : {inputPath}");
        Console.WriteLine($"Salida  : {outputPath}");
        Console.WriteLine($"OZD     : {files.Length}");
        Console.WriteLine($"Modo    : {(overwrite ? "sobrescribir" : "omitir existentes")}");
        Console.WriteLine();

        if (files.Length == 0)
        {
            Console.WriteLine("No se encontraron archivos .ozd.");
            return 0;
        }

        var reader = new OZDReader();
        var decoder = new BcDecoder();

        int converted = 0;
        int skipped = 0;
        int failed = 0;

        for (int i = 0; i < files.Length; i++)
        {
            string source = files[i];

            string relative = Path.GetRelativePath(inputPath, source);
            string relativePng = Path.ChangeExtension(relative, ".png");
            string destination = Path.Combine(outputPath, relativePng);

            Console.Write(
                $"[{i + 1,4}/{files.Length}] {relative} ... ");

            try
            {
                if (!overwrite && File.Exists(destination))
                {
                    skipped++;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("omitido");
                    Console.ResetColor();
                    continue;
                }

                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination)!);

                TextureData texture = await reader.Load(source);

                CompressionFormat format = texture.Format switch
                {
                    TextureSurfaceFormat.Dxt1 => CompressionFormat.Bc1,
                    TextureSurfaceFormat.Dxt3 => CompressionFormat.Bc2,
                    TextureSurfaceFormat.Dxt5 => CompressionFormat.Bc3,

                    _ => throw new NotSupportedException(
                        $"Formato no soportado: {texture.Format}")
                };

                using Image image =
                    decoder.DecodeRawToImageRgba32(
                        texture.Data,
                        texture.Width,
                        texture.Height,
                        format);

                await image.SaveAsPngAsync(destination);

                converted++;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"OK ({texture.Width}x{texture.Height}, {texture.Format})");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                failed++;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"        {ex.GetType().Name}: {ex.Message}");
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

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            OZD Batch Extractor

            Uso:
              dotnet run --project Tools\OzdBatchExtractor -- "<entrada>" ["<salida>"] [--overwrite]

            Ejemplos:
              dotnet run --project Tools\OzdBatchExtractor -- "C:\Data_Broyal\Data\Interface"

              dotnet run --project Tools\OzdBatchExtractor -- "C:\Data_Broyal\Data\Interface\GFx" "C:\Data_Broyal\OZD_Preview"

              dotnet run --project Tools\OzdBatchExtractor -- "C:\Data_Broyal\Data\Interface" "C:\Data_Broyal\OZD_Preview" --overwrite
            """);
    }
}
