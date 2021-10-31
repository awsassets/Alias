using Mono.Cecil.Cil;

public class SequencePointMessage
{
    public SequencePointMessage(string text, SequencePoint? sequencePoint)
    {
        Text = text;
        SequencePoint = sequencePoint;
    }

    public string Text { get; }
    public SequencePoint? SequencePoint { get; }
}