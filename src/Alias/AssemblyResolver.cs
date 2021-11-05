using Mono.Cecil;

public class AssemblyResolver : IAssemblyResolver
{
    Dictionary<string, AssemblyDefinition> cache = new(StringComparer.InvariantCultureIgnoreCase);

    ReaderParameters readerParameters = new(ReadingMode.Deferred)
    {
        ReadSymbols = false
    };

    public AssemblyResolver(IEnumerable<string> references)
    {
        var assemblyLocation = typeof(AssemblyResolver).Assembly.Location;
        var directory = Path.GetDirectoryName(assemblyLocation)!;
        var netStandardPath = Path.Combine(directory, "netstandard.dll");

        var netStandard = GetAssembly(netStandardPath);
        cache = new()
        {
            ["netstandard"] = netStandard,
            ["mscorlib"] = netStandard,
        };
        foreach (var reference in references)
        {
            var assembly = GetAssembly(reference);
            cache[assembly.Name.Name] = assembly;
        }
    }

    public void Add(ModuleDefinition module)
    {
        var assembly = module.Assembly;
        cache[assembly.Name.Name] = assembly;
    }

    AssemblyDefinition GetAssembly(string file)
    {
        try
        {
            return AssemblyDefinition.ReadAssembly(file, readerParameters);
        }
        catch (Exception exception)
        {
            throw new($"Could not read '{file}'.", exception);
        }
    }

    public AssemblyDefinition? Resolve(AssemblyNameReference name)
    {
        return Resolve(name, new());
    }

    public AssemblyDefinition? Resolve(AssemblyNameReference name, ReaderParameters? parameters)
    {
        if (cache.TryGetValue(name.Name, out var assembly))
        {
            return assembly;
        }

        return null;
    }

    public void Dispose()
    {
        foreach (var value in cache.Values)
        {
            value.Dispose();
        }
    }
}