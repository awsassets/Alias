using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

public class TypeCache
{
    static List<string> assemblyNames = new()
    {
        "mscorlib",
        "System",
        "System.Runtime",
        "System.Core",
        "netstandard"
    };

    public TypeCache(ModuleDefinition module, IAssemblyResolver resolver)
    {
        var assemblyDefinitions = new List<AssemblyDefinition>();
        foreach (var assemblyName in assemblyNames)
        {
            var assembly = ResolveIgnoreVersion(resolver, assemblyName);
            if (assembly == null)
            {
                continue;
            }

            assemblyDefinitions.Add(assembly);
        }

        Dictionary<string, TypeDefinition> types = new();

        foreach (var assembly in assemblyDefinitions)
        {
            foreach (var type in assembly.MainModule.GetTypes())
            {
                AddIfPublic(type, types);
            }
        }

        foreach (var assembly in assemblyDefinitions)
        {
            foreach (var exportedType in assembly.MainModule.ExportedTypes)
            {
                if (assemblyDefinitions.Any(x => x.Name.Name == exportedType.Scope.Name))
                {
                    continue;
                }

                var typeDefinition = exportedType.Resolve();
                if (typeDefinition == null)
                {
                    continue;
                }

                AddIfPublic(typeDefinition, types);
            }
        }
        
        ObjectReference = module.ImportReference(types["System.Object"]);

        StringReference = module.ImportReference(types["System.String"]);
    }

    public static AssemblyDefinition? ResolveIgnoreVersion(IAssemblyResolver resolver, string assemblyName)
    {
        return resolver.Resolve(new(assemblyName, null));
    }

    static void AddIfPublic(TypeDefinition typeDefinition, Dictionary<string, TypeDefinition> cachedTypes)
    {
        if (!typeDefinition.IsPublic)
        {
            return;
        }

        if (cachedTypes.ContainsKey(typeDefinition.FullName))
        {
            return;
        }

        cachedTypes.Add(typeDefinition.FullName, typeDefinition);
    }
    
    public TypeReference ObjectReference { get; }
    public TypeReference StringReference { get; }
}