using System;
using System.Globalization;
using System.Threading;
using Xunit;

public class Tests
{
    static WeavingResult weavingResult;

    static Tests()
    {
        weavingResult = WeavingHelper.CreateIsolatedAssemblyCopy(
            "AssemblyToProcess",
            new() { "AssemblyToReference" },
            new[]
            {
                "AssemblyToReference.dll",
                "de\\AssemblyToReference.resources.dll",
                "fr\\AssemblyToReference.resources.dll"
            });
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
    public void Simple()
    {
        var instance = weavingResult.GetInstance("ClassToTest");
        Assert.Equal("Hello", instance.Simple());
    }

    [Fact]
    public void ThrowException()
    {
        try
        {
            var instance = weavingResult.GetInstance("ClassToTest");
            instance.ThrowException();
        }
        catch (Exception exception)
        {
            Assert.Contains("ClassToReference.cs:line", exception.StackTrace);
        }
    }
}