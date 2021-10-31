using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Processor
{
    public string AssemblyFilePath = null!;
    public string IntermediateDirectory = null!;
    public string? KeyFilePath;
    public bool SignAssembly;
    public bool DelaySign;
    public string ProjectDirectory = null!;
    public string References = null!;

    public ILogger Logger = null!;
    public List<string> PackAssemblies = null!;

    public virtual bool Execute()
    {
        var assembly = typeof(Processor).Assembly;

        Logger.LogInfo($"Alias (version {assembly.GetName().Version} @ {assembly.CodeBase}) Executing");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            Inner();
            return !Logger.ErrorOccurred;
        }
        catch (WeavingException exception)
        {
            Logger.LogError(exception.Message);
            return false;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception.ToString());
            return false;
        }
        finally
        {
            stopwatch.Stop();
            Logger.LogInfo($"Finished AssemblyPack {stopwatch.ElapsedMilliseconds}ms.");
        }
    }

    void Inner()
    {
        ValidateProjectPath();
        ValidateAssemblyPath();

        InnerExecute();
    }

    public void Cancel()
    {
    }
}
