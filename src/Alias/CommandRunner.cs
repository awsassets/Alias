using CommandLine;

public static class CommandRunner
{
    public static IEnumerable<Error> RunCommand(Invoke invoke, params string[] args)
    {
        var arguments = Parser.Default.ParseArguments<Options>(args);

        if (arguments is Parsed<Options> parsed)
        {
            var parsedValue = parsed.Value;
            var targetDirectory = FindTargetDirectory(parsedValue.TargetDirectory);
            invoke(targetDirectory, parsedValue.AssembliesToAlias, parsedValue.Key);
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