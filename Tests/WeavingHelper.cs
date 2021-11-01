using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VerifyTests;

public static class WeavingHelper
{
    public static Assembly CreateIsolatedAssemblyCopy(
        string inputAssemblyName,
        List<string> includeAssemblies,
        string[] references)
    {
        var currentDirectory = AssemblyDirectoryHelper.GetCurrentDirectory();
        
        var fullPathReferences = references.Select(x => Path.Combine(currentDirectory, x)).ToList();
#if(NET472)
        fullPathReferences.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\mscorlib.dll");
#else
        fullPathReferences.Add(Path.Combine(Nuget.PackagesPath, @"netstandard.library\2.0.3\build\netstandard2.0\ref\netstandard.dll"));
#endif

        var inputAssemblyPath = Path.Combine(currentDirectory, inputAssemblyName + ".dll");

        var processor = new Processor(new MockBuildLogger())
        {
            AssemblyPath = inputAssemblyPath,
            IntermediateDirectory = currentDirectory,
            References = string.Join(";", fullPathReferences),
            AssembliesToAlias = includeAssemblies,
            SignAssembly = true,
            KeyFilePath = Path.Combine(AttributeReader.GetSolutionDirectory(), "Key.snk")
        };

        processor.Execute();

        return Assembly.Load(File.ReadAllBytes(inputAssemblyPath));
    }
}