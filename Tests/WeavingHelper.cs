using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VerifyTests;

public static class WeavingHelper
{
    public static WeavingResult CreateIsolatedAssemblyCopy(
        string inputAssemblyName,
        List<string> includeAssemblies,
        string[] references)
    {
        var currentDirectory = AssemblyDirectoryHelper.GetCurrentDirectory();

        using var assemblyResolver = new TestAssemblyResolver();

        var fullPathReferences = references.Select(x => Path.Combine(currentDirectory, x)).ToList();
        fullPathReferences.Add(typeof(string).Assembly.Location);

        var inputAssemblyPath = Path.Combine(currentDirectory, inputAssemblyName + ".dll");


        var testStatus = new WeavingResult();

        var processor = new Processor
        {
            AssemblyPath = inputAssemblyPath,
            IntermediateDirectory = currentDirectory,
            Logger = new MockBuildLogger(),
            References = string.Join(";", fullPathReferences),
            PackAssemblies = includeAssemblies,
            SignAssembly = true,
            KeyFilePath = Path.Combine(AttributeReader.GetSolutionDirectory(), "Key.snk")
        };

        processor.Execute();

        testStatus.Assembly = Assembly.Load(File.ReadAllBytes(inputAssemblyPath));
        return testStatus;
    }
}