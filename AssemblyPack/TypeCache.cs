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

        VoidReference = module.ImportReference(types["System.Void"]);

        var stringReference = module.ImportReference(types["System.String"]);
        var dictionary = types["System.Collections.Generic.Dictionary`2"];
        var dictionaryOfString = module.ImportReference(dictionary);
        DictionaryOfStringAdd = module.ImportReference(dictionaryOfString.Resolve().Methods.First(m => m.Name == "Add"))
            .MakeHostInstanceGeneric(stringReference, stringReference);

        var compilerGeneratedAttribute = types["System.Runtime.CompilerServices.CompilerGeneratedAttribute"];
        CompilerGeneratedAttributeCtor = module.ImportReference(compilerGeneratedAttribute.Methods.First(x => x.IsConstructor));
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

    public TypeReference VoidReference { get; }

    public MethodReference CompilerGeneratedAttributeCtor { get; }

    public MethodReference DictionaryOfStringAdd { get; }
}