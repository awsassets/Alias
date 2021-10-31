using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;

public partial class ModuleWeaver
{
    ConstructorInfo instructionConstructorInfo = typeof(Instruction).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(OpCode), typeof(object) }, null);
    TypeDefinition targetType = null!;
    TypeDefinition sourceType = null!;
    TypeDefinition commonType = null!;
    MethodDefinition attachMethod = null!;
    MethodDefinition loaderCctor = null!;
    FieldDefinition assemblyNamesField = null!;
    FieldDefinition symbolNamesField = null!;

    void ImportAssemblyLoader()
    {
        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = ModuleDefinition.AssemblyResolver,
            ReadSymbols = true,
            SymbolReaderProvider = new PdbReaderProvider()
        };

        using var resourceStream = GetType().Assembly.GetManifestResourceStream("Template.dll");
        var moduleDefinition = ModuleDefinition.ReadModule(resourceStream, readerParameters);

        sourceType = moduleDefinition.Types.Single(x => x.Name == "ILTemplate");
        commonType = moduleDefinition.Types.Single(x => x.Name == "Common");

        targetType = new("AssemblyPack", "AssemblyLoader", sourceType.Attributes, Resolve(sourceType.BaseType));
        targetType.CustomAttributes.Add(new(TypeCache.CompilerGeneratedAttributeCtor));
        ModuleDefinition.Types.Add(targetType);
        CopyFields(sourceType);
        CopyMethod(sourceType.Methods.Single(x => x.Name == "ResolveAssembly"));

        loaderCctor = CopyMethod(sourceType.Methods.Single(x => x.IsConstructor && x.IsStatic));
        attachMethod = CopyMethod(sourceType.Methods.Single(x => x.Name == "Attach"));
    }

    void CopyFields(TypeDefinition source)
    {
        foreach (var field in source.Fields)
        {
            var newField = new FieldDefinition(field.Name, field.Attributes, Resolve(field.FieldType));
            targetType.Fields.Add(newField);
            if (field.Name == "assemblyNames")
            {
                assemblyNamesField = newField;
            }

            if (field.Name == "symbolNames")
            {
                symbolNamesField = newField;
            }
        }
    }

    TypeReference Resolve(TypeReference baseType)
    {
        var typeDefinition = baseType.Resolve();
        var typeReference = ModuleDefinition.ImportReference(typeDefinition);
        if (baseType is ArrayType)
        {
            return new ArrayType(typeReference);
        }

        if (baseType.IsGenericInstance)
        {
            typeReference = typeReference.MakeGenericInstanceType(baseType.GetGenericInstanceArguments().ToArray());
        }

        return typeReference;
    }

    MethodDefinition CopyMethod(MethodDefinition templateMethod, bool makePrivate = false)
    {
        var attributes = templateMethod.Attributes;
        if (makePrivate)
        {
            attributes &= ~Mono.Cecil.MethodAttributes.Public;
            attributes |= Mono.Cecil.MethodAttributes.Private;
        }

        var returnType = Resolve(templateMethod.ReturnType);
        var newMethod = new MethodDefinition(templateMethod.Name, attributes, returnType)
        {
            IsPInvokeImpl = templateMethod.IsPInvokeImpl,
            IsPreserveSig = templateMethod.IsPreserveSig,
        };
        if (templateMethod.IsPInvokeImpl)
        {
            var moduleRef = ModuleDefinition.ModuleReferences.FirstOrDefault(mr => mr.Name == templateMethod.PInvokeInfo.Module.Name);
            if (moduleRef is null)
            {
                moduleRef = new(templateMethod.PInvokeInfo.Module.Name);
                ModuleDefinition.ModuleReferences.Add(moduleRef);
            }

            newMethod.PInvokeInfo = new(templateMethod.PInvokeInfo.Attributes, templateMethod.PInvokeInfo.EntryPoint, moduleRef);
        }

        if (templateMethod.Body is not null)
        {
            newMethod.Body.InitLocals = templateMethod.Body.InitLocals;
            foreach (var variableDefinition in templateMethod.Body.Variables)
            {
                var newVariableDefinition = new VariableDefinition(Resolve(variableDefinition.VariableType));
                newMethod.Body.Variables.Add(newVariableDefinition);
            }

            CopyInstructions(templateMethod, newMethod);
            CopyExceptionHandlers(templateMethod, newMethod);
        }

        foreach (var parameterDefinition in templateMethod.Parameters)
        {
            var newParameterDefinition = new ParameterDefinition(Resolve(parameterDefinition.ParameterType))
            {
                Name = parameterDefinition.Name
            };
            newMethod.Parameters.Add(newParameterDefinition);
        }

        targetType.Methods.Add(newMethod);
        return newMethod;
    }

    void CopyExceptionHandlers(MethodDefinition templateMethod, MethodDefinition newMethod)
    {
        if (!templateMethod.Body.HasExceptionHandlers)
        {
            return;
        }

        foreach (var exceptionHandler in templateMethod.Body.ExceptionHandlers)
        {
            var handler = new ExceptionHandler(exceptionHandler.HandlerType);
            var templateInstructions = templateMethod.Body.Instructions;
            var targetInstructions = newMethod.Body.Instructions;
            if (exceptionHandler.TryStart is not null)
            {
                handler.TryStart = targetInstructions[templateInstructions.IndexOf(exceptionHandler.TryStart)];
            }

            if (exceptionHandler.TryEnd is not null)
            {
                handler.TryEnd = targetInstructions[templateInstructions.IndexOf(exceptionHandler.TryEnd)];
            }

            if (exceptionHandler.HandlerStart is not null)
            {
                handler.HandlerStart = targetInstructions[templateInstructions.IndexOf(exceptionHandler.HandlerStart)];
            }

            if (exceptionHandler.HandlerEnd is not null)
            {
                handler.HandlerEnd = targetInstructions[templateInstructions.IndexOf(exceptionHandler.HandlerEnd)];
            }

            if (exceptionHandler.FilterStart is not null)
            {
                handler.FilterStart = targetInstructions[templateInstructions.IndexOf(exceptionHandler.FilterStart)];
            }

            if (exceptionHandler.CatchType is not null)
            {
                handler.CatchType = Resolve(exceptionHandler.CatchType);
            }

            newMethod.Body.ExceptionHandlers.Add(handler);
        }
    }

    void CopyInstructions(MethodDefinition templateMethod, MethodDefinition newMethod)
    {
        var newBody = newMethod.Body;
        var newInstructions = newBody.Instructions;
        var newDebugInformation = newMethod.DebugInformation;

        var templateDebugInformation = templateMethod.DebugInformation;

        foreach (var instruction in templateMethod.Body.Instructions)
        {
            var newInstruction = CloneInstruction(instruction);
            newInstructions.Add(newInstruction);
            var sequencePoint = templateDebugInformation.GetSequencePoint(instruction);
            if (sequencePoint is not null)
            {
                newDebugInformation.SequencePoints.Add(TranslateSequencePoint(newInstruction, sequencePoint));
            }
        }

        var scope = newDebugInformation.Scope = new(newInstructions.First(), newInstructions.Last());

        foreach (var variable in templateDebugInformation.Scope.Variables)
        {
            var targetVariable = newBody.Variables[variable.Index];

            scope.Variables.Add(new(targetVariable, variable.Name));
        }
    }

    Instruction CloneInstruction(Instruction instruction)
    {
        var newInstruction = (Instruction)instructionConstructorInfo.Invoke(new[] { instruction.OpCode, instruction.Operand });
        newInstruction.Operand = Import(instruction.Operand);
        return newInstruction;
    }

    SequencePoint? TranslateSequencePoint(Instruction instruction, SequencePoint? sequencePoint)
    {
        if (sequencePoint is null)
        {
            return null;
        }

        return new(instruction, sequencePoint.Document)
        {
            StartLine = sequencePoint.StartLine,
            StartColumn = sequencePoint.StartColumn,
            EndLine = sequencePoint.EndLine,
            EndColumn = sequencePoint.EndColumn,
        };
    }

    object Import(object operand)
    {
        if (operand is MethodReference reference)
        {
            var methodReference = reference;
            if (methodReference.DeclaringType == sourceType || methodReference.DeclaringType == commonType)
            {
                var mr = targetType.Methods.FirstOrDefault(x => x.Name == methodReference.Name && x.Parameters.Count == methodReference.Parameters.Count);
                if (mr is null)
                {
                    //little poetic license... :). .Resolve() doesn't work with "extern" methods
                    return CopyMethod(methodReference.DeclaringType.Resolve().Methods
                            .First(m => m.Name == methodReference.Name && m.Parameters.Count == methodReference.Parameters.Count),
                        methodReference.DeclaringType != sourceType);
                }

                return mr;
            }

            if (methodReference.DeclaringType.IsGenericInstance)
            {
                return ModuleDefinition.ImportReference(methodReference.Resolve())
                    .MakeHostInstanceGeneric(methodReference.DeclaringType.GetGenericInstanceArguments().ToArray());
            }

            return ModuleDefinition.ImportReference(methodReference.Resolve());
        }

        if (operand is TypeReference typeReference)
        {
            return Resolve(typeReference);
        }

        if (operand is FieldReference fieldReference)
        {
            return targetType.Fields.FirstOrDefault(f => f.Name == fieldReference.Name)
                   ?? new FieldReference(fieldReference.Name, ModuleDefinition.ImportReference(fieldReference.FieldType.Resolve()), ModuleDefinition.ImportReference(fieldReference.DeclaringType.Resolve()));
        }

        return operand;
    }
}