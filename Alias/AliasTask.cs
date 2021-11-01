using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Alias;

public class AliasTask :
    Task
{
    Processor processor = null!;

    [Required]
    public string AssemblyPath { set; get; } = null!;

    [Required]
    public string IntermediateDirectory { get; set; } = null!;

    public string? KeyOriginatorFile { get; set; }
    public string? AssemblyOriginatorKeyFile { get; set; }

    public bool SignAssembly { get; set; }
    public bool DelaySign { get; set; }
    
    [Required]
    public ITaskItem[] ReferenceCopyLocalFiles { get; set; } = null!;

    [Required]
    public ITaskItem[] PackAssemblies { get; set; } = null!;

    [Required]
    public string References { get; set; } = null!;
    
    [Output]
    public ITaskItem[] NewReferencePaths { get; set; } = null!;

    public override bool Execute()
    {
        var buildLogger = new BuildLogger(BuildEngine);
        var assembly = typeof(Processor).Assembly;

        buildLogger.LogInfo($"Alias (version {assembly.GetName().Version} @ {assembly.CodeBase}) Executing");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            processor = new(
                buildLogger, 
                AssemblyPath,
                IntermediateDirectory,
                References)
            {
                KeyFilePath = KeyOriginatorFile ?? AssemblyOriginatorKeyFile,
                SignAssembly = SignAssembly,
                DelaySign = DelaySign,
                AssembliesToAlias = PackAssemblies.Select(x => x.ItemSpec).ToList()
            };
            processor.Execute();
            return !buildLogger.ErrorOccurred;
        }
        catch (WeavingException exception)
        {
            buildLogger.LogError(exception.Message);
            return false;
        }
        catch (Exception exception)
        {
            buildLogger.LogError(exception.ToString());
            return false;
        }
        finally
        {
            stopwatch.Stop();
            buildLogger.LogInfo($"Finished Alias {stopwatch.ElapsedMilliseconds}ms.");
        }
    }
}