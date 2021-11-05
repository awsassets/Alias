using Mono.Cecil;

public class AssemblyResolver : IAssemblyResolver
{
    Dictionary<string, AssemblyDefinition> assemblyDefinitionCache = new(StringComparer.InvariantCultureIgnoreCase);

    ReaderParameters readerParameters = new(ReadingMode.Deferred)
    {
        ReadSymbols = false
    };

    public AssemblyResolver()
    {
        var assemblyLocation = typeof(AssemblyResolver).Assembly.Location;
        var directory = Path.GetDirectoryName(assemblyLocation)!;
        var netStandardPath = Path.Combine(directory, "netstandard.dll");

        assemblyDefinitionCache = new()
        {
            ["netstandard"] = GetAssembly(netStandardPath)
        };
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

    public AssemblyDefinition? Resolve(AssemblyNameReference assemblyNameReference)
    {
        return Resolve(assemblyNameReference, new());
    }

    public AssemblyDefinition? Resolve(AssemblyNameReference assemblyNameReference, ReaderParameters? parameters)
    {
        if (assemblyDefinitionCache.TryGetValue(assemblyNameReference.Name, out var assembly))
        {
            return assembly;
        }

        return null;
    }

    public void Dispose()
    {
        foreach (var value in assemblyDefinitionCache.Values)
        {
            value.Dispose();
        }
    }
}