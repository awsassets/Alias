using CommandLine;

public class Options
{
    [Option('t', "target-directory", Required = false)]
    public string? TargetDirectory { get; set; }

    [Option('a', "assemblies", Required = true)]
    public string Assemblies { get; set; } = null!;

    [Option('k', "key", Required = false)]
    public string? Key { get; set; }
}