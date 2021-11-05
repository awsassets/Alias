﻿using Alias;
using Mono.Cecil;
using StrongNameKeyPair = Mono.Cecil.StrongNameKeyPair;

public static class Program
{
    static int Main(string[] args)
    {
        var errors = CommandRunner.RunCommand(
            (targetDirectory, assemblyNamesToAliases, keyFile) =>
            {
                Console.WriteLine($"TargetDirectory: {targetDirectory}");
                Console.WriteLine($"AssembliesToAlias: {assemblyNamesToAliases}");
                Console.WriteLine($"KeyFile: {keyFile}");

                Inner(targetDirectory, assemblyNamesToAliases.Split(";"), keyFile);
            },
            args);

        if (errors.Any())
        {
            return 1;
        }

        return 0;
    }

    public static void Inner(string targetDirectory, IEnumerable<string> assemblyNamesToAliases, string? keyFile)
    {
        if (!Directory.Exists(targetDirectory))
        {
            throw new ErrorException($"Target directory does not exist: {targetDirectory}");
        }

        StrongNameKeyPair? keyPair = null;
        var publicKey = Array.Empty<byte>();
        if (keyFile != null)
        {
            if (!File.Exists(keyFile))
            {
                throw new ErrorException($"KeyFile directory does not exist: {keyFile}");
            }

            var fileBytes = File.ReadAllBytes(keyFile);

            keyPair = new(fileBytes);
            publicKey = keyPair.PublicKey;
        }

        var allFiles = Directory.GetFiles(targetDirectory, "*.dll").ToList();

        var assembliesToPatch = allFiles
            .Select(x => new FileAssembly(Path.GetFileNameWithoutExtension(x), x))
            .ToList();

        var assembliesToAlias = new List<AssemblyAlias>();

        foreach (var assemblyToAlias in assemblyNamesToAliases)
        {
            if (string.IsNullOrWhiteSpace(assemblyToAlias))
            {
                throw new ErrorException("Empty string in assembliesToAliasString");
            }

            static void ProcessItem(List<FileAssembly> fileAssemblies, FileAssembly item, List<AssemblyAlias> assemblyAliases)
            {
                fileAssemblies.Remove(item);
                assemblyAliases.Add(new(item.Name, item.Path, item.Name + "_Alias", item.Path.Replace(".dll", "_Alias.dll")));
            }

            if (assemblyToAlias.EndsWith("*"))
            {
                var match = assemblyToAlias.TrimEnd('*');
                foreach (var item in assembliesToPatch.Where(x => x.Name.StartsWith(match)).ToList())
                {
                    ProcessItem(assembliesToPatch, item, assembliesToAlias);
                }
            }
            else
            {
                var item = assembliesToPatch.SingleOrDefault(x => x.Name == assemblyToAlias);
                if (item == null)
                {
                    throw new ErrorException($"Could not find {assemblyToAlias} in {targetDirectory}.");
                }

                ProcessItem(assembliesToPatch, item, assembliesToAlias);
            }
        }

        using var resolver = new AssemblyResolver();
        {
            foreach (var assembly in assembliesToAlias)
            {
                var assemblyTargetPath = assembly.TargetPath;
                File.Delete(assemblyTargetPath);
                var (module, hasSymbols) = ModuleReaderWriter.Read(assembly.SourcePath, resolver);

                var name = module.Assembly.Name;
                name.Name += "_Alias";
                FixKey(keyPair, name);
                Redirect(module, assembliesToAlias, publicKey);
                ModuleReaderWriter.Write(keyPair, hasSymbols, module, assemblyTargetPath);
                module.Dispose();
            }

            foreach (var assembly in assembliesToPatch)
            {
                var assemblyPath = assembly.Path;
                var (module, hasSymbols) = ModuleReaderWriter.Read(assemblyPath, resolver);

                FixKey(keyPair, module.Assembly.Name);
                Redirect(module, assembliesToAlias, publicKey);
                ModuleReaderWriter.Write(keyPair, hasSymbols, module, assemblyPath);
                module.Dispose();
            }
        }
        foreach (var assembly in assembliesToAlias)
        {
            File.Delete(assembly.SourcePath);
        }
    }

    static void FixKey(StrongNameKeyPair? key, AssemblyNameDefinition name)
    {
        if (key == null)
        {
            name.Hash = Array.Empty<byte>();
            name.PublicKey = Array.Empty<byte>();
            name.PublicKeyToken = Array.Empty<byte>();
        }
        else
        {
            name.PublicKey = key.PublicKey;
        }
    }

    static void Redirect(ModuleDefinition targetModule, List<AssemblyAlias> aliases, byte[] publicKey)
    {
        var assemblyReferences = targetModule.AssemblyReferences;
        foreach (var alias in aliases)
        {
            var toChange = assemblyReferences.SingleOrDefault(x => x.Name == alias.SourceName);
            if (toChange != null)
            {
                toChange.Name = alias.TargetName;
                toChange.PublicKey = publicKey;
            }
        }
    }
}
