using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Build.Framework;
using Mono.Cecil.Cil;

public class WeavingResult
{
    List<LogMessage> messages = new();

    internal void AddMessage(string text, MessageImportance messageImportance)
    {
        var message = new LogMessage(text, messageImportance);
        messages.Add(message);
    }

    public IReadOnlyList<LogMessage> Messages => messages;

    List<SequencePointMessage> warnings = new();

    internal void AddWarning(string text, SequencePoint? sequencePoint)
    {
        var message = new SequencePointMessage(text, sequencePoint);
        warnings.Add(message);
    }

    public IReadOnlyList<SequencePointMessage> Warnings => warnings;

    List<SequencePointMessage> errors = new();

    internal void AddError(string text, SequencePoint? sequencePoint)
    {
        var message = new SequencePointMessage(text, sequencePoint);
        errors.Add(message);
    }

    public IReadOnlyList<SequencePointMessage> Errors => errors;
    public Assembly Assembly { get; internal set; } = null!;
    public string AssemblyPath { get; internal set; } = null!;

    public dynamic GetInstance(string className)
    {
        var type = Assembly.GetType(className, true)!;
        return Activator.CreateInstance(type)!;
    }
}