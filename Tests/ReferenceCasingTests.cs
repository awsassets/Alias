using VerifyXunit;

[UsesVerify]
public class ReferenceCasingTests : BasicTests 
{
    static WeavingResult weavingResult;

    static ReferenceCasingTests()
    {
        weavingResult = WeavingHelper.CreateIsolatedAssemblyCopy(
            "AssemblyToProcess.dll",
            new(){ "AssemblyToReference" },
            new[] { "AssemblyToReference.dll" },
            assemblyName: "ReferenceCasing");
    }

    public override WeavingResult WeavingResult => weavingResult;
}
