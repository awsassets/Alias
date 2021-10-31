using AssemblyToReference;

public static class ClassToReference
{
    public static string Simple() => "Hello";

    public static void ThrowException()
    {
        throw new("Hello");
    }

    public static string InternationalFoo() => strings.Hello;
}
