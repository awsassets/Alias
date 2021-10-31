using System;
using System.IO;
using System.Linq;
using DummyAssembly;
using Xunit;

public class AssemblyResolverTests
{
    ILogger logger = new MockBuildLogger();

    [Fact]
    public void ShouldFindReferenceByAssemblyName()
    {
        var assemblyPath = Path.GetTempFileName();
        try
        {
            var assembly = typeof(Class1).Assembly;
            File.Copy(assembly.Location, assemblyPath, true);

            var resolver = new AssemblyResolver(logger, new[] {assemblyPath});
            using var resolvedAssembly = TypeCache.ResolveIgnoreVersion(resolver, assembly.GetName().Name!);
            Assert.Equal(assembly.FullName, resolvedAssembly!.FullName);
        }
        finally
        {
            File.Delete(assemblyPath);
        }
    }

    [Fact]
    public void ShouldReturnNullWhenTheAssemblyIsNotFound()
    {
        var resolver = new AssemblyResolver(logger, Enumerable.Empty<string>());
        Assert.Null(TypeCache.ResolveIgnoreVersion(resolver,"SomeNonExistingAssembly"));
    }

    [Fact]
    public void ShouldGuessTheAssemblyNameFromTheFileNameIfTheAssemblyCannotBeLoaded()
    {
        var resolver = new AssemblyResolver(logger, new[] { @"AssemblyPack\BadAssembly.dll" });
        Assert.ThrowsAny<Exception>(() => TypeCache.ResolveIgnoreVersion(resolver,"BadAssembly"));
    }
}
