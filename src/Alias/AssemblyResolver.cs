using Mono.Cecil;

public class AssemblyResolver : IAssemblyResolver
{
    Dictionary<string, string> referenceDictionary;
    Dictionary<string, AssemblyDefinition> assemblyDefinitionCache = new(StringComparer.InvariantCultureIgnoreCase);

    public AssemblyResolver()
    {
        var assemblyLocation = typeof(AssemblyResolver).Assembly.Location;
        var directory = Path.GetDirectoryName(assemblyLocation)!;
        referenceDictionary = new()
        {
            ["netstandard"] = Path.Combine(directory, "netstandard.dll")
        };
    }
    
    AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
    {
        if (assemblyDefinitionCache.TryGetValue(file, out var assembly))
        {
            return assembly;
        }

        parameters.AssemblyResolver ??= this;
        try
        {
            return assemblyDefinitionCache[file] = AssemblyDefinition.ReadAssembly(file, parameters);
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
        parameters ??= new();

        if (referenceDictionary.TryGetValue(assemblyNameReference.Name, out var fileFromDerivedReferences))
        {
            return GetAssembly(fileFromDerivedReferences, parameters);
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