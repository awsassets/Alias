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
    ILogger logger;
    string assemblyPath;
    string intermediateDirectory;
    string references;
    IAssemblyResolver assemblyResolver = null!;
    TypeCache TypeCache = null!;
    string? keyFile;
    bool signAssembly;
    bool delaySign;
    List<string> assembliesToAlias;

    public Processor(
        ILogger logger,
        string assemblyPath,
        string intermediateDirectory,
        string references,
        string? keyFile,
        bool signAssembly,
        bool delaySign,
        List<string> assembliesToAlias)
    {
        this.logger = logger;
        this.assemblyPath = assemblyPath;
        this.intermediateDirectory = intermediateDirectory;
        this.references = references;
        this.keyFile = keyFile;
        this.signAssembly = signAssembly;
        this.delaySign = delaySign;
        this.assembliesToAlias = assembliesToAlias;
    }

    public void Execute()
    {
        try
        {
            SplitUpReferences();
            assemblyResolver = new AssemblyResolver(logger, SplitReferences);
            ReadModule();
            var infoClassName = GetInfoClassName();
            if (ModuleDefinition.Types.Any(x => x.Name == infoClassName))
            {
                logger.LogWarning($"Already processed by Alias. Path: {assemblyPath}");
                return;
            }

            TypeCache = new(ModuleDefinition, assemblyResolver);
            AddWeavingInfo(infoClassName);
            FindStrongNameKey();
            foreach (var reference in SplitReferences)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(reference);
                if (!assembliesToAlias.Contains(assemblyName))
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

                module.Write(Path.Combine(intermediateDirectory, name.Name + ".dll"), parameters);
            }

            Redirect(ModuleDefinition);

            WriteModule();
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
        foreach (var packAssembly in assembliesToAlias)
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