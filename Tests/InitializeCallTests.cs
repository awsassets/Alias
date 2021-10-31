﻿using VerifyXunit;

[UsesVerify]
public class InitializeCallTests : BasicTests
{
    static WeavingResult weavingResult;

    static InitializeCallTests()
    {
        weavingResult = WeavingHelper.CreateIsolatedAssemblyCopy(
            "AssemblyToProcess.dll",
            new(){ "AssemblyToReference" },
            new[] { "AssemblyToReference.dll" },
            "InitializeCall");
    }

    public override WeavingResult WeavingResult => weavingResult;
}