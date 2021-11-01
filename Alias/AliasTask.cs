using System;
using System.Collections.Generic;
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
    public ITaskItem[] AssembliesToAlias { get; set; } = null!;

    [Required]
    public string References { get; set; } = null!;

    [Output]
    public ITaskItem[] PathsToRemove { get; set; } = null!;

    public override bool Execute()
    {
        var buildLogger = new BuildLogger(BuildEngine);
        var assembly = typeof(Processor).Assembly;

        buildLogger.LogInfo($"Alias (version {assembly.GetName().Version} @ {assembly.CodeBase}) Executing");

        var stopwatch = Stopwatch.StartNew();
        Debugger.Launch();
        try
        {
            processor = new(
                buildLogger,
                AssemblyPath,
                IntermediateDirectory,
                References,
                KeyOriginatorFile ?? AssemblyOriginatorKeyFile,
                SignAssembly,
                DelaySign,
                AssembliesToAlias.Select(x => x.ItemSpec).ToList());
            var replacements = processor.Execute();
            var pathsToRemove = new List<ITaskItem>();

            foreach (var replacement in replacements)
            {
                var taskItem = ReferenceCopyLocalFiles.SingleOrDefault(x => x.ItemSpec == replacement.From);
                if (taskItem != null)
                {
                    pathsToRemove.Add(taskItem);
                }
            }

            PathsToRemove = pathsToRemove.ToArray();

            return true;
        }
        catch (ErrorException exception)
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