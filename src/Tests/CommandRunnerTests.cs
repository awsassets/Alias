using CommandLine;
using VerifyXunit;
using Xunit;

[UsesVerify]
public class CommandRunnerTests
{
    [Fact]
    public Task MissingAssembliesToAlias()
    {
        var result = Parse("--target-directory", "directory");
        return Verifier.Verify(result);
    }

    [Fact]
    public Task All()
    {
        var result = Parse("--target-directory", "directory","--key", "theKey", "--assemblies-to-alias", "assembly");
        return Verifier.Verify(result);
    }

    [Fact]
    public Task CurrentDirectory()
    {
        var result = Parse("--assemblies-to-alias", "assembly");
        return Verifier.Verify(result);
    }

    [Fact]
    public Task MultipleAssemblies()
    {
        var result = Parse("--assemblies-to-alias", "assembly1;assembly2");
        return Verifier.Verify(result);
    }

    static Result Parse(params string[] input)
    {
        string? directory = null;
        string? key = null;
        string? assembliesToAlias = null;
        var result = CommandRunner.RunCommand(
            (_targetDirectory, _assembliesToAlias, _key) =>
            {
                directory = _targetDirectory;
                key = _key;
                assembliesToAlias = _assembliesToAlias;
            },
            input);
        return new(result, directory, key, assembliesToAlias);
    }

    public record Result(IEnumerable<Error> errors, string? directory, string? key, string? assembliesToAlias);
}