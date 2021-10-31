using System;
using Xunit;

public class ReferenceMissingTests
{
    [Fact]
    public void ThrowsForMissingReference()
    {
        // Note: this will throw WeavingException because References is null, but should actually
        // log an error about the missing assembly
        Assert.Throws<Exception>(() =>
        {
            WeavingHelper.CreateIsolatedAssemblyCopy(
                "AssemblyToProcess",
                new(){ "AssemblyToReference","MissingAssembly" },
                new[] { "AssemblyToReference.dll" },
                "InitializeCall");
        });
    }
}
