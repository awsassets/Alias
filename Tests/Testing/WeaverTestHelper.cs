using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

public static class WeaverTestHelper
{
    public static WeavingResult ExecuteTestRun(
        this Processor weaver,
        string assemblyPath,
        bool runPeVerify = true,
        string? assemblyName = null,
        IEnumerable<string>? ignoreCodes = null)
    {
        assemblyPath = Path.GetFullPath(assemblyPath);
        var tempDir = Path.Combine(Path.GetDirectoryName(assemblyPath)!, "AssemblyPackTemp");
        Directory.CreateDirectory(tempDir);

        string targetFileName;
        if (assemblyName == null)
        {
            assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            targetFileName = Path.GetFileName(assemblyPath);
        }
        else
        {
            var extension = Path.GetExtension(assemblyPath);
            targetFileName = assemblyName + (string.IsNullOrEmpty(extension) ? ".dll" : extension);
        }

        var targetAssemblyPath = Path.Combine(tempDir, targetFileName);
        File.Delete(targetAssemblyPath);

        using var assemblyResolver = new TestAssemblyResolver();

        var testStatus = new WeavingResult();
        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = assemblyResolver,
            SymbolReaderProvider = new SymbolReaderProvider(),
            ReadWrite = false,
            ReadSymbols = true,
        };

        using (var module = ModuleDefinition.ReadModule(assemblyPath, readerParameters))
        {
            var typeCache = new TypeCache(module, assemblyResolver);
            weaver.AssemblyFilePath = assemblyPath;
            weaver.TypeCache = typeCache;
            module.Assembly.Name.Name = assemblyName;
            weaver.ModuleDefinition = module;

            weaver.Execute();
        }

        if (runPeVerify && IsWindows())
        {
            List<string> ignoreList;
            if (ignoreCodes == null)
            {
                ignoreList = new();
            }
            else
            {
                ignoreList = ignoreCodes.ToList();
            }

            PeVerifier.ThrowIfDifferent(assemblyPath, targetAssemblyPath, ignoreList, Path.GetDirectoryName(assemblyPath));
        }

        testStatus.Assembly = Assembly.Load(File.ReadAllBytes(targetAssemblyPath));
        testStatus.AssemblyPath = targetAssemblyPath;
        return testStatus;
    }

    static bool IsWindows()
    {
        var platform = Environment.OSVersion.Platform.ToString();
        return platform.StartsWith("win", StringComparison.OrdinalIgnoreCase);
    }
}