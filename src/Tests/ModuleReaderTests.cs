using Alias;
using Xunit;

public class ModuleReaderTests 
{
    [Fact]
    public void WithSymbols()
    {
        var assemblyPath = Path.Combine(Environment.CurrentDirectory, "DummyAssembly.dll");
        var result = ModuleReaderWriter.Read(assemblyPath, new AssemblyResolver());
        Assert.NotNull(result.module);
        Assert.True(result.hasSymbols);
    }

    [Fact]
    public void NoSymbols()
    {
        var assemblyPath = Path.Combine(Environment.CurrentDirectory, "AssemblyWithNoSymbols.dll");
        var result = ModuleReaderWriter.Read(assemblyPath, new AssemblyResolver());
        Assert.NotNull(result.module);
        Assert.False(result.hasSymbols);
    }
}