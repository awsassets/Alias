using CommandLine;

public static class CommandRunner
{
    public static IEnumerable<Error> RunCommand(Invoke invoke, params string[] args)
    {
        var arguments = Parser.Default.ParseArguments<Options>(args);

        if (arguments is NotParsed<Options> errors)
        {
            return errors.Errors;
        }

        var parsed = (Parsed<Options>) arguments;

        var options = parsed.Value;
        var targetDirectory = FindTargetDirectory(options.TargetDirectory);
        Console.WriteLine($"TargetDirectory: {targetDirectory}");
        var keyFile = options.Key;

        if (keyFile != null)
        {
            keyFile = Path.GetFullPath(keyFile);
            Console.WriteLine($"KeyFile: {keyFile}");
            if (!File.Exists(keyFile))
            {
                throw new ErrorException($"KeyFile directory does not exist: {keyFile}");
            }
        }

        Console.WriteLine("AssembliesToAlias:");
        foreach (var assemblyToAlias in options.AssembliesToAlias)
        {
            Console.WriteLine($" * {assemblyToAlias}");
        }

        var references = options.References.ToList();
        var referencesFile = Path.Combine(targetDirectory, "alias-references.txt");
        if (File.Exists(referencesFile))
        {
            references.AddRange(File.ReadAllLines(referencesFile));
        }

        if (options.ReferenceFile != null && File.Exists(options.ReferenceFile))
        {
            references.AddRange(File.ReadAllLines(options.ReferenceFile));
        }

        if (references.Any())
        {
            Console.WriteLine("References:");
            foreach (var reference in references)
            {
                Console.WriteLine($" * {reference}");
            }
        }

        invoke(targetDirectory, options.AssembliesToAlias, references, keyFile);
        return Enumerable.Empty<Error>();
    }

    static string FindTargetDirectory(string? targetDirectory)
    {
        if (targetDirectory == null)
        {
            return Environment.CurrentDirectory;
        }

        return Path.GetFullPath(targetDirectory);
    }
}