using VerifyXunit;

[UsesVerify]
public class ReferenceCasingTests : BasicTests 
{
    static WeavingResult weavingResult;

    static ReferenceCasingTests()
    {
        weavingResult = WeavingHelper.CreateIsolatedAssemblyCopy(
            inputAssemblyName:"AssemblyToProcess",
            new(){ "AssemblyToReference" },
            new[] { "AssemblyToReference.dll" },
            outputAssemblyName: "ReferenceCasing");
    }

    public override WeavingResult WeavingResult => weavingResult;
}
