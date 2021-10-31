using System.Collections.Generic;

public partial class ModuleWeaver
{
    bool includeDebugSymbols;
    bool ignoreSatelliteAssemblies;
    List<string> includeAssemblies;

    public ModuleWeaver(
        bool includeDebugSymbols,
        bool ignoreSatelliteAssemblies,
        List<string> includeAssemblies)
    {
        this.includeDebugSymbols = includeDebugSymbols;
        this.ignoreSatelliteAssemblies = ignoreSatelliteAssemblies;
        this.includeAssemblies = includeAssemblies;
    }

    public void Execute()
    {
        FixResourceCase();
        EmbedResources();

        ImportAssemblyLoader();
        CallAttach();

        BuildUpNameDictionary();
    }
}
