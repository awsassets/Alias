using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
// ReSharper disable CommentTypo

static class Common
{
    [Conditional("DEBUG")]
    public static void Log(string format, params object[] args)
    {
        // Should this be trace?
        Debug.WriteLine("=== AssemblyPack === " + string.Format(format, args));
    }

    static byte[] ReadStream(Stream stream)
    {
        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        return data;
    }

    public static Assembly ReadExistingAssembly(AssemblyName name)
    {
        var currentDomain = AppDomain.CurrentDomain;
        var assemblies = currentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var currentName = assembly.GetName();
            if (string.Equals(currentName.Name, name.Name, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(CultureToString(currentName.CultureInfo), CultureToString(name.CultureInfo), StringComparison.InvariantCultureIgnoreCase))
            {
                Log("Assembly '{0}' already loaded, returning existing assembly", assembly.FullName);

                return assembly;
            }
        }

        return null;
    }

    static string CultureToString(CultureInfo culture)
    {
        if (culture is null)
        {
            return "";
        }

        return culture.Name;
    }

    public static Assembly ReadFromEmbeddedResources(Dictionary<string, string> assemblyNames, Dictionary<string, string> symbolNames, AssemblyName requestedAssemblyName)
    {
        var name = requestedAssemblyName.Name.ToLowerInvariant();

        if (requestedAssemblyName.CultureInfo is not null && !string.IsNullOrEmpty(requestedAssemblyName.CultureInfo.Name))
        {
            name = $"{requestedAssemblyName.CultureInfo.Name}.{name}";
        }

        byte[] assemblyData;
        using (var assemblyStream = LoadStream(assemblyNames, name))
        {
            if (assemblyStream is null)
            {
                return null;
            }

            assemblyData = ReadStream(assemblyStream);
        }

        using (var pdbStream = LoadStream(symbolNames, name))
        {
            if (pdbStream is not null)
            {
                var pdbData = ReadStream(pdbStream);
                return Assembly.Load(assemblyData, pdbData);
            }
        }

        return Assembly.Load(assemblyData);
    }

    static Stream LoadStream(Dictionary<string, string> resourceNames, string name)
    {
        if (resourceNames.TryGetValue(name, out var value))
        {
            return LoadStream(value);
        }

        return null;
    }

    static Stream LoadStream(string fullName)
    {
        var executingAssembly = Assembly.GetExecutingAssembly();

        return executingAssembly.GetManifestResourceStream(fullName);
    }
}
