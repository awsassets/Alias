using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using ManifestResourceAttributes = Mono.Cecil.ManifestResourceAttributes;

public partial class ModuleWeaver : IDisposable
{
    public ModuleDefinition ModuleDefinition { get; set; } = null!;

    public TypeCache TypeCache { get; set; } = null!;

    public string AssemblyFilePath { get; set; } = null!;

    public string References { get; set; } = null!;

    public ILogger Logger { get; set; } = null!;
    List<Stream> streams = new();

    void EmbedResources()
    {
        var references = GetReferences();
        
        var normalReferences = GetFilteredReferences(references).ToList();
        if (normalReferences.Any())
        {
            Logger.LogInfo("\tIncluding references");

            foreach (var reference in normalReferences)
            {
                var referencePath = reference.FullPath;
                var relativePrefix = reference.GetResourceNamePrefix("assemblypack.");

                if (reference.IsResourcesAssembly && ignoreSatelliteAssemblies)
                {
                    continue;
                }

                Embed(relativePrefix, referencePath);

                if (includeDebugSymbols)
                {
                    var pdbFullPath = Path.ChangeExtension(referencePath, "pdb");
                    if (File.Exists(pdbFullPath))
                    {
                        Embed(relativePrefix, pdbFullPath);
                    }
                }
            }
        }
    }

    bool CompareAssemblyName(string matchText, string assemblyName)
    {
        if (matchText.EndsWith("*") && matchText.Length > 1)
        {
            return assemblyName.StartsWith(matchText.Substring(0, matchText.Length - 1), StringComparison.OrdinalIgnoreCase);
        }

        return matchText.Equals(assemblyName, StringComparison.OrdinalIgnoreCase);
    }

    List<Reference> GetReferences()
    {
        var references = new List<Reference>();
        
        // Add all references, but mark them as special
        var splittedReferences = References.Split(';').Where(item => !string.IsNullOrEmpty(item));

        foreach (var splittedReference in splittedReferences)
        {
            var fileName = splittedReference;

            if (!Path.IsPathRooted(fileName))
            {
                fileName = Path.GetFullPath(fileName);
            }

            if (!File.Exists(fileName))
            {
                continue;
            }

            if (references.Any(x => x.FullPath == fileName))
            {
                continue;
            }

            var reference = new Reference(fileName);

            references.Add(reference);
        }

        return references;
    }

    IEnumerable<Reference> GetFilteredReferences(IEnumerable<Reference> references)
    {
        var includeList = includeAssemblies;
        if (includeList.Any())
        {
            var skippedAssemblies = new List<string>(includeAssemblies);

            foreach (var reference in references)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(reference.FileName);

                if (includeList.Any(x => CompareAssemblyName(x, assemblyName)))
                {
                    skippedAssemblies.Remove(includeList.First(x => CompareAssemblyName(x, assemblyName)));
                    yield return reference;

                    // Make sure to embed resources, even if not explicitly included
                    if (!ignoreSatelliteAssemblies)
                    {
                        var resourcesAssemblyName = assemblyName + ".resources";
                        var resourcesAssemblyReferences = (from x in references
                                                           where x.IsResourcesAssembly && CompareAssemblyName(x.FileNameWithoutExtension, resourcesAssemblyName)
                                                           select x).ToList();
                        foreach (var resourcesAssemblyReference in resourcesAssemblyReferences)
                        {
                            yield return resourcesAssemblyReference;
                        }
                    }
                }
            }

            if (skippedAssemblies.Count > 0)
            {
                foreach (var skippedAssembly in skippedAssemblies)
                {
                    throw new($"Assembly '{skippedAssembly}' cannot be found, please update the configuration");
                }

                throw new WeavingException("One or more errors occurred, please check the log");
            }
        }
    }
    
    void Embed(string prefix, string fullPath)
    {
        try
        {
            InnerEmbed(prefix, fullPath);
        }
        catch (Exception exception)
        {
            throw new(
                innerException: exception,
                message: $@"Failed to embed.
prefix: {prefix}
fullPath: {fullPath}");
        }
    }

    void InnerEmbed(string prefix, string fullPath)
    {
        var resourceName = $"{prefix}{Path.GetFileName(fullPath).ToLowerInvariant()}";

        if (ModuleDefinition.Resources.Any(x => string.Equals(x.Name, resourceName, StringComparison.OrdinalIgnoreCase)))
        {
            // an assembly that is already embedded uncompressed, using <EmbeddedResource> in the project file
            throw new WeavingException($"'{fullPath}' is already embedded");
        }

        Logger.LogInfo($"\t\tEmbedding '{fullPath}'");

        var memoryStream = OpenRead(fullPath);
        streams.Add(memoryStream);
        var resource = new EmbeddedResource(resourceName, ManifestResourceAttributes.Private, memoryStream);
        ModuleDefinition.Resources.Add(resource);
    }
    
    public static FileStream OpenRead(string path)
    {
        return new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
    }

    public void Dispose()
    {
        foreach (var stream in streams)
        {
            stream.Dispose();
        }
    }
}
