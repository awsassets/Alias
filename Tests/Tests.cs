using System;
using System.Reflection;
using Xunit;

public class Tests
{
    static Assembly assembly;

    static Tests()
    {
        assembly = WeavingHelper.CreateIsolatedAssemblyCopy(
            "AssemblyToProcess",
            new() { "AssemblyToReference" },
            new[]
            {
                "AssemblyToReference.dll",
                "de\\AssemblyToReference.resources.dll",
                "fr\\AssemblyToReference.resources.dll"
            });
    }

    //TODO:
    //[Fact]
    //public void UsingResource()
    //{
    //    var culture = Thread.CurrentThread.CurrentUICulture;
    //    try
    //    {
    //        Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("fr-FR");
    //        var instance1 = weavingResult.GetInstance("ClassToTest");
    //        Assert.Equal("Salut", instance1.InternationalFoo());
    //    }
    //    finally
    //    {
    //        Thread.CurrentThread.CurrentUICulture = culture;
    //    }
    //}

    [Fact]
    public void Simple()
    {
        var instance = assembly.GetInstance("ClassToTest");
        Assert.Equal("Hello", instance.Simple());
    }

    [Fact]
    public void ThrowException()
    {
        try
        {
            var instance = assembly.GetInstance("ClassToTest");
            instance.ThrowException();
        }
        catch (Exception exception)
        {
            Assert.Contains("ClassToReference.cs:line", exception.StackTrace);
        }
    }
}