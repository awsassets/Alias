using System;
using System.Linq;
using Mono.Cecil;

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
            var weavingInfoClassName = GetWeavingInfoClassName();
            if (ModuleDefinition.Types.Any(x => x.Name == weavingInfoClassName))
            {
                Logger.LogWarning($"The assembly has already been processed by AssemblyPack. Weaving aborted. Path: {AssemblyFilePath}");
                return;
            }
            TypeCache = new(ModuleDefinition,assemblyResolver);
            var moduleWeaver = new ModuleWeaver(true,true,PackAssemblies)
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

    string GetWeavingInfoClassName()
    {
        var classPrefix = ModuleDefinition.Assembly.Name.Name.Replace(".", "");
        return $"{classPrefix}_ProcessedByAssemblyPack";
    }
}
