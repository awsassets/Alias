using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class Ildasm
{
    static string? ildasmPath;

    static Ildasm()
    {
        FoundIldasm = SdkToolFinder.TryFindTool("ildasm", out ildasmPath);
    }

    public static readonly bool FoundIldasm;

    public static string Decompile(string assemblyPath, string? item = "")
    {
        if (!FoundIldasm)
        {
            throw new("Could not find find ildasm.exe.");
        }

        if (!string.IsNullOrEmpty(item))
        {
            item = $"/item:{item}";
        }

        var startInfo = new ProcessStartInfo(
            fileName: ildasmPath,
            arguments: $"\"{assemblyPath}\" /text /linenum /source {item}")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(startInfo);
        var stringBuilder = new StringBuilder();
        string? line;
        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            line = line.Trim();

            if (line.Length == 0)
            {
                continue;
            }

            if (line.Contains(".line "))
            {
                continue;
            }

            if (line.Contains(".custom instance void ["))
            {
                continue;
            }

            if (line.StartsWith("// "))
            {
                continue;
            }

            if (line.StartsWith("//0"))
            {
                continue;
            }

            if (line.StartsWith("  } // end of "))
            {
                stringBuilder.AppendLine("  } ");
                continue;
            }

            if (line.StartsWith("} // end of "))
            {
                stringBuilder.AppendLine("}");
                continue;
            }

            if (line.StartsWith("    .get instance "))
            {
                continue;
            }

            if (line.StartsWith("    .set instance "))
            {
                continue;
            }

            if (line.Contains(".language '"))
            {
                continue;
            }

            stringBuilder.AppendLine(line);
        }

        return stringBuilder.ToString();
    }

    public static string DecompileAssemblyLoader(string afterAssemblyPath)
    {
        var decompile = Decompile(afterAssemblyPath, "AssemblyPack.AssemblyLoader");
        var checksumPattern = new Regex(@"^(\s*IL_[0123456789abcdef]{4}:  ldstr\s*"")[0123456789ABCDEF]{32,40}""", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        return string.Join(Environment.NewLine, decompile.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Where(l => !l.StartsWith("// ", StringComparison.CurrentCulture) && !string.IsNullOrEmpty(l))
            .Select(l => checksumPattern.Replace(l, e => e.Groups[1].Value + "[CHECKSUM]\""))
            .ToList());
    }
}
