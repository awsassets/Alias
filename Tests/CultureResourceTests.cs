using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

[UsesVerify]
public class CultureResourceTests : BaseTest
{
    static WeavingResult weavingResult;

    static CultureResourceTests()
    {
        weavingResult = WeavingHelper.CreateIsolatedAssemblyCopy(
            "AssemblyToProcess",
            new() { "AssemblyToReference" },
            new[]
            {
                "AssemblyToReference.dll",
                "de\\AssemblyToReference.resources.dll",
                "fr\\AssemblyToReference.resources.dll"
            },
            "Culture");
    }

    [Fact]
    public void UsingResource()
    {
        var culture = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("fr-FR");
            var instance1 = weavingResult.GetInstance("ClassToTest");
            Assert.Equal("Salut", instance1.InternationalFoo());
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }

    [Fact]
    public Task TemplateHasCorrectSymbols()
    {
        var text = Ildasm.DecompileAssemblyLoader(WeavingResult.AssemblyPath);
        return Verifier.Verify(text)
            .UniqueForAssemblyConfiguration()
            .UniqueForRuntime();
    }

    public override WeavingResult WeavingResult => weavingResult;
}