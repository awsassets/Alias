using System.Collections.Generic;
using System.IO;

public static class WeavingHelper
{
    public static WeavingResult CreateIsolatedAssemblyCopy(
        string assemblyPath, 
        List<string> includeAssemblies, 
        string[] references,
        string assemblyName)
    {
        var currentDirectory = AssemblyDirectoryHelper.GetCurrentDirectory();

        if (!Path.IsPathRooted(assemblyPath))
        {
            assemblyPath = Path.Combine(currentDirectory, assemblyPath);
        }
        
        var processor = new Processor
        {
            AssemblyFilePath = assemblyPath,
            Logger = new MockBuildLogger(),
            References = string.Join(";", references),
            PackAssemblies = includeAssemblies
        };

        return processor.ExecuteTestRun(
            assemblyPath,
            assemblyName: assemblyName,
            ignoreCodes: new []{ "0x80131869" },
            runPeVerify:false);
    }
}
