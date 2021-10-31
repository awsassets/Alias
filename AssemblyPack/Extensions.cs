using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

static class Extensions
{
    public static Collection<TypeReference> GetGenericInstanceArguments(this TypeReference type) => ((GenericInstanceType)type).GenericArguments;

    public static MethodReference MakeHostInstanceGeneric(this MethodReference self, params TypeReference[] args)
    {
        var reference = new MethodReference(
            self.Name,
            self.ReturnType,
            self.DeclaringType.MakeGenericInstanceType(args))
        {
            HasThis = self.HasThis,
            ExplicitThis = self.ExplicitThis,
            CallingConvention = self.CallingConvention
        };

        foreach (var parameter in self.Parameters)
        {
            reference.Parameters.Add(new(parameter.ParameterType));
        }

        foreach (var genericParam in self.GenericParameters)
        {
            reference.GenericParameters.Add(new(genericParam.Name, reference));
        }

        return reference;
    }

    public static void InsertBefore(this Collection<Instruction> instructions, int index, params Instruction[] newInstructions)
    {
        foreach (var item in newInstructions)
        {
            instructions.Insert(index, item);
            index++;
        }
    }
}
