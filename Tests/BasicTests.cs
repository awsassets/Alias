using System;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

public abstract class BasicTests : BaseTest
{
    [Fact]
    public void Simple()
    {
        var instance = WeavingResult.GetInstance("ClassToTest");
        Assert.Equal("Hello", instance.Simple());
    }

    [Fact]
    public void ThrowException()
    {
        try
        {
            var instance = WeavingResult.GetInstance("ClassToTest");
            instance.ThrowException();
        }
        catch (Exception exception)
        {
            Assert.Contains("ClassToReference.cs:line", exception.StackTrace);
        }
    }

    [Fact]
    public void TypeReferencedWithPartialAssemblyNameIsLoadedFromExistingAssemblyInstance()
    {
        var instance = WeavingResult.GetInstance("ClassToTest");
        var assemblyLoadedByCompileTimeReference = instance.GetReferencedAssembly();
        var typeName = "ClassToReference, AssemblyToReference";
        if (WeavingResult.Assembly.GetName().Name!.EndsWith("35"))
        {
            typeName += "35";
        }
        var typeLoadedWithPartialAssemblyName = Type.GetType(typeName);
        Assert.NotNull(typeLoadedWithPartialAssemblyName);

        Assert.Equal(assemblyLoadedByCompileTimeReference, typeLoadedWithPartialAssemblyName!.Assembly);
    }

    [Fact]
    public Task TemplateHasCorrectSymbols()
    {
        var text = Ildasm.DecompileAssemblyLoader(WeavingResult.AssemblyPath);
        return Verifier.Verify(text).UniqueForAssemblyConfiguration().UniqueForRuntime();
    }
}
