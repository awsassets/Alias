using System;
using System.IO;

static class Nuget
{
    public static string PackagesPath;

    static Nuget()
    {
        var nugetPackagesEnv = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (nugetPackagesEnv != null)
        {
            PackagesPath = nugetPackagesEnv;
        }
        else
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            PackagesPath =Path.Combine(userProfile, @".nuget\packages");
        }
    }
}