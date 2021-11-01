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
    public ITaskItem[] PackAssemblies{ get; set; } = null!;

    [Required]
    public string References { get; set; } = null!;

    public override bool Execute()
    {
        var buildLogger = new BuildLogger(BuildEngine);

        processor = new()
        {
            Logger = buildLogger,
            AssemblyPath = AssemblyPath,
            IntermediateDirectory = IntermediateDirectory,
            KeyFilePath = KeyOriginatorFile ?? AssemblyOriginatorKeyFile,
            SignAssembly = SignAssembly,
            DelaySign = DelaySign,
            References = References,
            PackAssemblies = PackAssemblies.Select(x => x.ItemSpec).ToList()
        };

        return processor.Execute();
    }
}