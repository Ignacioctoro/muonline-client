using MUnique.OpenMU.Network.Packets.ServerToClient;

var type = typeof(AddCharacterToScopeExtendedRef);

Console.WriteLine($"Tipo: {type.FullName}");
Console.WriteLine();

foreach (var property in type.GetProperties())
{
    Console.WriteLine($"{property.Name} : {property.PropertyType}");
}
