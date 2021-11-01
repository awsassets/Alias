using Mono.Cecil;

public partial class Processor
{
    public ModuleDefinition ModuleDefinition = null!;
    bool hasSymbols;

    public void ReadModule()
    {
        var result = ReadModule(AssemblyPath, assemblyResolver);
        hasSymbols = result.hasSymbols;
        if (!hasSymbols)
        {
            logger.LogInfo("Module has no debug symbols.");
        }

        ModuleDefinition = result.module;
    }

    public static (ModuleDefinition module, bool hasSymbols) ReadModule(string assemblyFile, IAssemblyResolver resolver)
    {
        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = resolver,
            InMemory = true
        };

        var module = ModuleDefinition.ReadModule(assemblyFile, readerParameters);

        var hasSymbols = false;
        try
        {
            module.ReadSymbols();
            hasSymbols = true;
        }
        catch
        {
        }

        return (module, hasSymbols);
    }
}