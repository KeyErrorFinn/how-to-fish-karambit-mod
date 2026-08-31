using Mono.Cecil;

if (args.Length is < 1 or > 3)
{
    Console.Error.WriteLine("Usage: AssemblyExplorer <Assembly-CSharp.dll> [Type.Method] [overload index]");
    return 2;
}

using var assembly = AssemblyDefinition.ReadAssembly(args[0]);
if (args.Length >= 2)
{
    var explicitSeparator = args[1].IndexOf("::", StringComparison.Ordinal);
    var separator = explicitSeparator >= 0 ? explicitSeparator : args[1].LastIndexOf('.');
    var typeName = args[1][..separator];
    var methodName = args[1][(separator + (explicitSeparator >= 0 ? 2 : 1))..];
    var type = assembly.MainModule.Types.SelectMany(Flatten).Single(type => type.FullName == typeName);
    var methods = type.Methods.Where(method => method.Name == methodName).ToList();
    for (var index = 0; index < methods.Count; index++)
        Console.WriteLine($"[{index}] {methods[index].FullName}");
    var selectedIndex = args.Length == 3 ? int.Parse(args[2]) : 0;
    var method = methods[selectedIndex];
    Console.WriteLine($"Selected [{selectedIndex}]\n");
    foreach (var instruction in method.Body.Instructions)
        Console.WriteLine($"{instruction.Offset:X4}: {instruction}");
    return 0;
}

var terms = new[] { "knife", "weapon", "equip", "skin", "cosmetic", "inventory", "item" };

foreach (var type in assembly.MainModule.Types
             .SelectMany(Flatten)
             .Where(type => terms.Any(term => type.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)))
             .OrderBy(type => type.FullName))
{
    Console.WriteLine(type.FullName);
    foreach (var field in type.Fields)
        Console.WriteLine($"  field {field.FieldType.FullName} {field.Name}");
    foreach (var method in type.Methods)
        Console.WriteLine($"  method {method.ReturnType.FullName} {method.Name}({string.Join(", ", method.Parameters.Select(parameter => parameter.ParameterType.FullName + " " + parameter.Name))})");
}

return 0;

static IEnumerable<TypeDefinition> Flatten(TypeDefinition type)
{
    yield return type;
    foreach (var nested in type.NestedTypes.SelectMany(Flatten))
        yield return nested;
}
