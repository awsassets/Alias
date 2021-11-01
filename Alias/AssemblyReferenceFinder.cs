using System;
using System.Collections.Generic;
using System.Linq;

public partial class Processor
{
    public List<string> SplitReferences = null!;

    public void SplitUpReferences()
    {
        SplitReferences = references
            .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        logger.LogDebug("Reference count: " + SplitReferences.Count);

        var joinedReferences = string.Join(Environment.NewLine + "  ", SplitReferences.OrderBy(x => x));
        logger.LogDebug($"References:{Environment.NewLine} {joinedReferences}");
    }
}
