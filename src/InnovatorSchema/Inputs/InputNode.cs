using System;
using System.Collections.Generic;

namespace InnovatorSchema;

internal abstract class InputNode
{
    public abstract InputKind Kind { get; }

    public abstract InputFormat Format { get; }

    public virtual bool CanReadObject => Kind == InputKind.Object;

    public virtual bool CanReadArray => Kind == InputKind.Array;

    public virtual bool Validate(ValidationContext context)
    {
        return true;
    }

    public virtual IReadOnlyList<InputMember> Members => Array.Empty<InputMember>();

    public virtual IReadOnlySet<string>? DuplicateProperties => null;

    public virtual IReadOnlyList<InputNode> Elements => Array.Empty<InputNode>();

    public virtual string Text => throw new InvalidOperationException("This is not a text value.");

    public virtual bool Boolean => throw new InvalidOperationException("This is not a boolean.");

    public virtual InputNode Property(string name)
    {
        return MissingInputNode.Instance;
    }

    public virtual InputNode Attribute(string name)
    {
        return MissingInputNode.Instance;
    }

    public virtual InputNode Collection(
        string container, string item, string attribute, string value)
    {
        return MissingInputNode.Instance;
    }
}

internal sealed class MissingInputNode : InputNode
{
    public static MissingInputNode Instance { get; } = new();

    public override InputKind Kind => InputKind.Missing;

    public override InputFormat Format => InputFormat.Json;
}
