using System;

public partial class ModuleWeaver
{
    void FixResourceCase()
    {
        foreach (var resource in ModuleDefinition.Resources)
        {
            if (resource.Name.StartsWith("assemblypack.", StringComparison.OrdinalIgnoreCase))
            {
                resource.Name = resource.Name.ToLowerInvariant();
            }
        }
    }
}