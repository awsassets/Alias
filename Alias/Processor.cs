using System;
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
            foreach (var reference in SplitReferences)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(reference);
                if (!PackAssemblies.Contains(assemblyName))
                {
                    continue;
                }

                var (module, hasSymbols) = ReadModule(reference, assemblyResolver);

                var name = module.Assembly.Name;
                name.Name += "_Alias";
                if (PublicKey == null)
                {
                    name.PublicKey = Array.Empty<byte>();
                }
                else
                {
                    name.PublicKey = PublicKey;
                }

                Redirect(module);
                
                var parameters = new WriterParameters
                {
                    StrongNameKeyPair = StrongNameKeyPair,
                    WriteSymbols = hasSymbols
                };

                module.Write(Path.Combine(IntermediateDirectory, name.Name + ".dll"), parameters);
            }

            Redirect(ModuleDefinition);

            WriteModule();
            ModuleDefinition?.Dispose();
        }
        finally
        {
            ModuleDefinition?.Dispose();
            assemblyResolver?.Dispose();
        }
    }

    void Redirect(ModuleDefinition targetModule)
    {
        var assemblyReferences = targetModule.AssemblyReferences;
        foreach (var packAssembly in PackAssemblies)
        {
            var toChange = assemblyReferences.SingleOrDefault(x => x.Name == packAssembly);
            //TODO: throw for invalid ref
            if (toChange != null)
            {
                toChange.Name += "_Alias";
                if (PublicKey == null)
                {
                    toChange.PublicKey = Array.Empty<byte>();
                }
                else
                {
                    toChange.PublicKey = PublicKey;
                }
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