namespace InnovatorSchema;

internal sealed class TextInputNode : InputNode
{
    public TextInputNode(string text)
    {
        Text = text;
    }

    public override InputKind Kind => InputKind.String;

    public override InputFormat Format => InputFormat.Xml;

    public override string Text { get; }
}
