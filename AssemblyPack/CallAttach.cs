using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

public partial class ModuleWeaver
{
    void CallAttach()
    {
        const MethodAttributes attributes = MethodAttributes.Private
                                            | MethodAttributes.HideBySig
                                            | MethodAttributes.Static
                                            | MethodAttributes.SpecialName
                                            | MethodAttributes.RTSpecialName;

        var moduleClass = ModuleDefinition.Types.FirstOrDefault(x => x.Name == "<Module>");
        if (moduleClass is null)
        {
            throw new WeavingException("Found no module class!");
        }
        var cctor = moduleClass.Methods.FirstOrDefault(x => x.Name == ".cctor");
        if (cctor is null)
        {
            cctor = new(".cctor", attributes, TypeCache.VoidReference);
            cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            moduleClass.Methods.Add(cctor);
        }
        cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, attachMethod));
    }
}
