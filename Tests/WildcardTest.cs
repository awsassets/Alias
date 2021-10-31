using System.Linq;
using Xunit;

public class WildcardTest
{
    [Fact]
    public void WeavesWildcards()
    {
        var wildcardWeave = WeavingHelper.CreateIsolatedAssemblyCopy(
            "AssemblyToProcess.dll",
            new(){"AssemblyToReference*" },
            new[] { "AssemblyToReference.dll"},
            "WildcardWeave");

        var referencedAssemblies = wildcardWeave.Assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();
        Assert.Contains("AssemblyToReference", referencedAssemblies);

        var instance = wildcardWeave.GetInstance("ClassToTest");
        Assert.Equal("Hello", instance.Simple());
    }
}
