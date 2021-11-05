using CommandLine;

public static class CommandRunner
{
    public static IEnumerable<Error> RunCommand(Invoke invoke, params string[] args)
    {
        var arguments = Parser.Default.ParseArguments<Options>(args);

        if (arguments is Parsed<Options> parsed)
        {
            var options = parsed.Value;
            var targetDirectory = FindTargetDirectory(options.TargetDirectory);
            Console.WriteLine($"TargetDirectory: {targetDirectory}");
            Console.WriteLine($"KeyFile: {options.Key}");
            Console.WriteLine($"AssembliesToAlias: {options.AssembliesToAlias}");
            foreach (var assemblyToAlias in options.AssembliesToAlias)
            {
                Console.WriteLine($" * {assemblyToAlias}");
            }

            invoke(targetDirectory, options.AssembliesToAlias, options.Key);
            return Enumerable.Empty<Error>();
        }

        return ((NotParsed<Options>) arguments).Errors;
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