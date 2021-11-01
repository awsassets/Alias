using System.Diagnostics;
using Mono.Cecil;

public partial class Processor
{
    public virtual void WriteModule()
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogDebug($"Writing assembly to '{assemblyPath}'.");

        var parameters = new WriterParameters
        {
            StrongNameKeyPair = StrongNameKeyPair,
            WriteSymbols = hasSymbols
        };

        ModuleDefinition.Assembly.Name.PublicKey = PublicKey;
        ModuleDefinition.Write(assemblyPath, parameters);
        logger.LogDebug($"Finished writing assembly {stopwatch.ElapsedMilliseconds}ms.");
    }
}
