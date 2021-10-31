using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public static class WeavingHelper
{
    public static WeavingResult CreateIsolatedAssemblyCopy(
        string inputAssemblyName,
        List<string> includeAssemblies,
        string[] references,
        string outputAssemblyName)
    {
        var currentDirectory = AssemblyDirectoryHelper.GetCurrentDirectory();

        var fullPathReferences = references.Select(x => Path.Combine(currentDirectory, x)).ToList();
        fullPathReferences.Add(typeof(string).Assembly.Location);
        var tempDir = Path.Combine(currentDirectory, "AssemblyPackTemp");
        Directory.CreateDirectory(tempDir);

        var inputAssemblyPath = Path.Combine(currentDirectory, inputAssemblyName + ".dll");
        var outputAssemblyPath = Path.Combine(tempDir, outputAssemblyName + ".dll");
        File.Delete(outputAssemblyPath);
        File.Copy(inputAssemblyPath, outputAssemblyPath);

        using var assemblyResolver = new TestAssemblyResolver();

        var testStatus = new WeavingResult();

        var processor = new Processor
        {
            AssemblyPath = outputAssemblyPath,
            IntermediateDirectory = tempDir,
            Logger = new MockBuildLogger(),
            References = string.Join(";", fullPathReferences),
            PackAssemblies = includeAssemblies
        };

        processor.Execute();

        if (IsWindows())
        {
            PeVerifier.ThrowIfDifferent(inputAssemblyPath, outputAssemblyPath, tempDir);
        }

        testStatus.Assembly = Assembly.Load(File.ReadAllBytes(outputAssemblyPath));
        testStatus.AssemblyPath = outputAssemblyPath;
        return testStatus;
    }

    static bool IsWindows()
    {
        var platform = Environment.OSVersion.Platform.ToString();
        return platform.StartsWith("win", StringComparison.OrdinalIgnoreCase);
    }
}