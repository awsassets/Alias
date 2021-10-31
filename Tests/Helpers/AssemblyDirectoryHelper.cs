using System;

static class AssemblyDirectoryHelper
{
    public static string GetCurrentDirectory()
    {
        // ReSharper disable once RedundantSuppressNullableWarningExpression
        return AppDomain.CurrentDomain.BaseDirectory!;
    }
}