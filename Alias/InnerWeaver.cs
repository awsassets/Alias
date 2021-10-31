using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

public partial class Processor
{
    public IAssemblyResolver assemblyResolver = null!;
    public TypeCache TypeCache = null!;
    public void InnerExecute()
    {
        try
        {
            SplitUpReferences();
            assemblyResolver = new AssemblyResolver(Logger, SplitReferences);
            ReadModule();
            var infoClassName = GetInfoClassName();
            if (ModuleDefinition.Types.Any(x => x.Name == infoClassName))
            {
                Logger.LogWarning($"Already processed by Alias. Path: {AssemblyFilePath}");
                return;
            }

            TypeCache = new(ModuleDefinition, assemblyResolver);
            AddWeavingInfo(infoClassName);
            var moduleWeaver = new ModuleWeaver(true, true, PackAssemblies)
            {
                ModuleDefinition = ModuleDefinition,
                AssemblyFilePath = AssemblyFilePath,
                References = References,
                Logger = Logger,
                TypeCache = TypeCache
            };
            moduleWeaver.Execute();
            FindStrongNameKey();
            WriteModule();
            ModuleDefinition?.Dispose();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception.ToString());
        }
        finally
        {
            ModuleDefinition?.Dispose();
            assemblyResolver?.Dispose();
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
