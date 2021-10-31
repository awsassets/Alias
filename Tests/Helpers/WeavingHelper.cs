using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class WeavingHelper
{
    public static WeavingResult CreateIsolatedAssemblyCopy(
        string assemblyPath, 
        List<string> includeAssemblies, 
        string[] references,
        string assemblyName)
    {
        var currentDirectory = AssemblyDirectoryHelper.GetCurrentDirectory();

        var weavingTask = new ModuleWeaver(true,false,includeAssemblies)
        {
            Logger = new MockBuildLogger(),
            References = string.Join(";", references.Select(r => Path.Combine(currentDirectory, r))),
        };

        if (!Path.IsPathRooted(assemblyPath))
        {
            assemblyPath = Path.Combine(currentDirectory, assemblyPath);
        }

        return weavingTask.ExecuteTestRun(
            assemblyPath,
            assemblyName: assemblyName,
            ignoreCodes: new []{ "0x80131869" },
            runPeVerify:false);
    }
}
