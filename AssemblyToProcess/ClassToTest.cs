﻿using System.Reflection;

public class ClassToTest
{
    public string Simple() => ClassToReference.Simple();

    public string InternationalFoo() => ClassToReference.InternationalFoo();
    
    public void ThrowException()
    {
        ClassToReference.ThrowException();
    }

    public Assembly GetReferencedAssembly() => typeof(ClassToReference).Assembly;
}
