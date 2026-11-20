using System.Collections.Generic;

namespace InnovatorSchema;

internal sealed class SequenceInputNode : InputNode
{
    public SequenceInputNode(IReadOnlyList<InputNode> elements)
    {
        Elements = elements;
    }

    public override InputKind Kind => InputKind.Array;

    public override InputFormat Format => InputFormat.Xml;

    public override IReadOnlyList<InputNode> Elements { get; }
}
