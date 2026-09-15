using Client.Data.BMD;

var path = args.Length > 0
    ? args[0]
    : throw new ArgumentException("Falta la ruta al BMD.");

if (!File.Exists(path))
    throw new FileNotFoundException("No se encontr� el BMD.", path);

var buffer = File.ReadAllBytes(path);

Console.WriteLine($"Archivo: {path}");
Console.WriteLine($"Bytes:   {buffer.Length}");
Console.WriteLine($"Header:  {BitConverter.ToString(buffer, 0, Math.Min(4, buffer.Length))}");

var reader = new BMDReader();
var bmd = reader.ReadFromBuffer(buffer);

Console.WriteLine();
Console.WriteLine("=== BMD ===");
Console.WriteLine($"Version : {bmd.Version}");
Console.WriteLine($"Name    : {bmd.Name}");
Console.WriteLine($"Meshes  : {bmd.Meshes.Length}");
Console.WriteLine($"Bones   : {bmd.Bones.Length}");
Console.WriteLine($"Actions : {bmd.Actions.Length}");
Console.WriteLine();
Console.WriteLine("=== TEXTURAS ===");

for (int i = 0; i < bmd.Meshes.Length; i++)
{
    Console.WriteLine(
        $"Mesh {i}: Texture={bmd.Meshes[i].Texture}, " +
        $"TexturePath='{bmd.Meshes[i].TexturePath}'");
}

Console.WriteLine();
Console.WriteLine("=== ACCIONES IMPORTANTES ===");

int[] important =
{
    0,
    1, 2, 3, 4, 5,
    15, 16,
    17, 18, 19, 20, 21, 22, 23, 24,
    25, 26, 27, 28, 29, 30, 31, 32, 33,
    34, 35,
    36, 37,
    239
};

foreach (var i in important)
{
    if (i >= bmd.Actions.Length)
    {
        Console.WriteLine($"Action {i}: NO EXISTE");
        continue;
    }

    var a = bmd.Actions[i];

    Console.WriteLine(
        $"Action {i,3}: Frames={a.NumAnimationKeys,3} " +
        $"LockPositions={a.LockPositions,-5} " +
        $"PlaySpeed={a.PlaySpeed}"
    );
}

Console.WriteLine();
Console.WriteLine("=== TODAS LAS ACCIONES ===");

for (int i = 0; i < bmd.Actions.Length; i++)
{
    var a = bmd.Actions[i];

    Console.WriteLine(
        $"{i,3}: Frames={a.NumAnimationKeys,3} " +
        $"Lock={a.LockPositions,-5} " +
        $"Speed={a.PlaySpeed}"
    );
}
