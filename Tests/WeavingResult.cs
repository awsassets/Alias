using System;
using System.Reflection;

public class WeavingResult
{
    public Assembly Assembly { get; internal set; } = null!;
    public string AssemblyPath { get; internal set; } = null!;

    public dynamic GetInstance(string className)
    {
        var type = Assembly.GetType(className, true)!;
        return Activator.CreateInstance(type)!;
    }
}