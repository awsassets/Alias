using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{
    void BuildUpNameDictionary()
    {
        foreach (var resource in ModuleDefinition.Resources.OrderBy(x => x.Name).Select(x => x.Name))
        {
            var parts = resource.Split('.');

            GetNameAndExt(parts, out var name, out var ext);

            if (!string.Equals(parts[0], "assemblypack", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(ext, "pdb", StringComparison.OrdinalIgnoreCase))
            {
                AddToDictionary(symbolNamesField, name, resource);
            }
            else
            {
                AddToDictionary(assemblyNamesField, name, resource);
            }
        }
    }

    static void GetNameAndExt(string[] parts, out string name, out string ext)
    {
        ext = parts[parts.Length - 1];

        name = string.Join(".", parts.Skip(1).Take(parts.Length - 2));
    }

    void AddToDictionary(FieldDefinition field, string key, string name)
    {
        var instructions = loaderCctor.Body.Instructions;
        var retIndex = instructions.Count - 1;
        instructions.InsertBefore(retIndex,
            Instruction.Create(OpCodes.Ldsfld, field),
            Instruction.Create(OpCodes.Ldstr, key),
            Instruction.Create(OpCodes.Ldstr, name),
            Instruction.Create(OpCodes.Callvirt, TypeCache.DictionaryOfStringAdd));
    }
}