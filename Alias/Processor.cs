using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

public partial class Processor
{
    IAssemblyResolver assemblyResolver = null!;
    TypeCache TypeCache = null!;
    public string AssemblyPath = null!;
    public string IntermediateDirectory = null!;
    public string? KeyFilePath;
    public bool SignAssembly;
    public bool DelaySign;
    public string References = null!;

    public ILogger Logger = null!;
    public List<string> PackAssemblies = null!;

    public void Execute()
    {
        try
        {
            SplitUpReferences();
            assemblyResolver = new AssemblyResolver(Logger, SplitReferences);
            ReadModule();
            var infoClassName = GetInfoClassName();
            if (ModuleDefinition.Types.Any(x => x.Name == infoClassName))
            {
                Logger.LogWarning($"Already processed by Alias. Path: {AssemblyPath}");
                return;
            }

            TypeCache = new(ModuleDefinition, assemblyResolver);
            AddWeavingInfo(infoClassName);
            FindStrongNameKey();
            var referenceModules = new List<(ModuleDefinition module, AssemblyNameDefinition nameDefinition)>();
            foreach (var reference in SplitReferences)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(reference);
                if (!PackAssemblies.Contains(assemblyName))
                {
                    continue;
                }

                var referenceModule = ReadModule(reference, assemblyResolver);
                var module = referenceModule.module;

                var name = new AssemblyNameDefinition($"{module.Assembly.Name.Name}_Alias", module.Assembly.Name.Version);
                if (PublicKey != null)
                {
                    name.PublicKey = PublicKey;
                }

                module.Assembly.Name = name;
                referenceModules.Add(new(module, name));
            }

            Redirect(ModuleDefinition, referenceModules);

            foreach (var referenceModule in referenceModules)
            {
                Redirect(referenceModule.module, referenceModules);
            }

            foreach (var referenceModule in referenceModules)
            {
                var moduleModule = referenceModule.module;
                var parameters = new WriterParameters
                {
                    StrongNameKeyPair = StrongNameKeyPair,
                    WriteSymbols = hasSymbols
                };

                moduleModule.Assembly.Name.PublicKey = PublicKey;
                moduleModule.Write(Path.Combine(IntermediateDirectory, moduleModule.Assembly.Name.Name + ".dll"), parameters);
            }

            WriteModule();
            ModuleDefinition?.Dispose();
        }
        finally
        {
            ModuleDefinition?.Dispose();
            assemblyResolver?.Dispose();
        }
    }

    void Redirect(ModuleDefinition targetModule, List<(ModuleDefinition module, AssemblyNameDefinition nameDefinition)> referenceModules)
    {
        var assemblyReferences = targetModule.AssemblyReferences;
        foreach (var packAssembly in PackAssemblies)
        {
            var toChange = assemblyReferences.SingleOrDefault(x => x.Name == packAssembly);
            //TODO: throw for invalid ref
            if (toChange != null)
            {
                var valueTuple = referenceModules.Single(x=>x.nameDefinition.Name.StartsWith(packAssembly+"_Alias"));
                toChange.Name = valueTuple.nameDefinition.Name;
                toChange.PublicKey = valueTuple.nameDefinition.PublicKey;
            }
        }
    }

    string GetInfoClassName()
    {
        var classPrefix = ModuleDefinition.Assembly.Name.Name.Replace(".", "");
        return $"{classPrefix}_ProcessedByAlias";
    }

    void AddWeavingInfo(string infoClassName)
    {
        const TypeAttributes typeAttributes = TypeAttributes.NotPublic | TypeAttributes.Class;
        var typeDefinition = new TypeDefinition(null, infoClassName, typeAttributes, TypeCache.ObjectReference);
        ModuleDefinition.Types.Add(typeDefinition);

        var attrs = typeof(Processor).Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute));
        var versionAttribute = (AssemblyFileVersionAttribute)attrs.FirstOrDefault();

        const FieldAttributes fieldAttributes = FieldAttributes.Assembly |
                                                FieldAttributes.Literal |
                                                FieldAttributes.Static |
                                                FieldAttributes.HasDefault;
        var field = new FieldDefinition("AliasVersion", fieldAttributes, TypeCache.StringReference)
        {
            Constant = versionAttribute.Version
        };

        typeDefinition.Fields.Add(field);
    }
}